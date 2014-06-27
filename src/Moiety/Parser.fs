namespace Moiety

module Parser = 

    open System
    open System.IO
    open System.Text
    open System.Collections.Generic

    type CharStream(stream:System.IO.Stream,encoding:System.Text.Encoding option) = 
        let buffersize = 0x90000
        let buffer = Array.init(buffersize) (fun _ -> char 0)
        let mutable max = 0
        let mutable pointer = 0
        let reader = 
            match encoding with
            | None -> new StreamReader(stream,true)
            | Some enc -> new StreamReader(stream,enc,true)
        let mutable reuseBuffer = []
        let mutable currentChar = (char 0) 
        
        let incrPointer = fun () -> 
                            currentChar <- buffer.[pointer]
                            pointer <- pointer + 1
                            true

        let fillBuffer() =
            match reader.EndOfStream with
            | true -> max <- 0; pointer <- 0;false
            | _ ->
                max <- reader.ReadBlock(buffer,0,buffersize)
                pointer <- 0
                true

        new (path:string) = new CharStream(new System.IO.FileStream(path,FileMode.Open),None)
        
        member this.CurrentEncoding
            with get() = reader.CurrentEncoding


        member this.Requeue(chars:char array) = 
            match chars with
            | [||] -> ignore()
            | _ ->
                match reuseBuffer with
                | [] -> reuseBuffer <- (chars |> List.ofArray)
                | _ -> reuseBuffer <- reuseBuffer @ (chars |> List.ofArray)

        member this.MoveNext() =
            match reuseBuffer with
            | h::t ->
                currentChar <- h
                reuseBuffer <- t
                true
            | [] ->
                if pointer >= max
                    then 
                        match fillBuffer() with
                        | false -> false
                        | true -> incrPointer()
                    else incrPointer()
                        
        member this.Current 
            with get() = currentChar

        member this.Reset() = 
            if reader.BaseStream.CanSeek
                then 
                    reader.BaseStream.Seek(0L,SeekOrigin.Begin) |> ignore
                    max <- 0
                    pointer <- 0
                else failwith "Cannot reset a stream that doesn't support seek"

        interface IDisposable with
            member this.Dispose() = reader.Dispose()

    type Settings(colDelimiter:string,rowDelimiter:string,honorQuotes:bool,maxFieldSize: int option) = 
        let colDelimList = colDelimiter.ToCharArray() |> List.ofArray
        let colDelimStart = colDelimList |> List.head
        let rowDelimList = rowDelimiter.ToCharArray() |> List.ofArray
        let rowDelimStart = rowDelimList |> List.head
        let rowIsNewline = not (rowDelimList |> List.exists (fun c -> c <> '\r' && c <> '\n'))
        let checkFieldSize = match maxFieldSize with
                             | None -> ignore
                             | Some max -> fun size -> if size <= max then ignore() else failwithf "Parsing aborted because the current field exceeds the maximum size limit of %i characters" max
    
        member this.ColumnDelimiter = colDelimiter
        member this.RowDelimiter = rowDelimiter
        member this.ColumnDelimiterList = colDelimList
        member this.RowDelimiterList = rowDelimList
        member this.ColDelimiterStart = colDelimStart
        member this.RowDelimiterStart = rowDelimStart
        member this.RowIsNewLine = rowIsNewline
        member this.HonorQuotes = honorQuotes
        member this.FieldMaxSize = maxFieldSize

        member this.IsFieldSizeOk = checkFieldSize
            

        member this.RowComparer =             
            let len = rowDelimiter.Length
            match this.RowIsNewLine,len with
            | true,_ -> (fun (stream:CharStream) firstChar -> 
                            match stream.MoveNext() with
                            | true when stream.Current = '\r' || stream.Current = '\n' -> true,[||]
                            | true -> true,[|stream.Current|]
                            | false -> true,[||])
            | false,l when l = 1 -> (fun _ _ -> true,[||])
            | _ -> (fun (stream:CharStream) (firstChar:char) -> 
                            let chars = [ 2..len ] |> List.choose (fun _ -> if stream.MoveNext() then Some stream.Current else None)
                            let fromFile = firstChar::chars
                            if rowDelimList = fromFile 
                                then true,[||]
                                else false,fromFile |> List.toArray)
            
        member this.ColumnComparer = 
            let len = colDelimiter.Length
            if len = 1
                then (fun _ _ -> true,[||])
                else (fun (stream:CharStream) (firstChar:char) -> 
                        let chars = [ 2..len ] |> List.choose (fun _ -> if stream.MoveNext() then Some stream.Current else None)
                        let fromFile = firstChar::chars
                        if colDelimList = fromFile
                            then true,[||]
                            else false,fromFile |> List.toArray)

    let defaultSettings = Settings(",","\r\n",true,None)    

    type FieldState = 
         | Valid of string
         | Invalid

    type FieldType = 
         | EndOfField of FieldState
         | EndOfRow of FieldState
         | EndOfFile of FieldState
                    

    type DelimiterType =
         | Row of char array
         | Column of char array
         | NoMatch of char array

    type FieldPosition = 
         | StartOfField
         | InField

    type FieldQuoteState = 
         | Ignore
         | IgnoreQuote
         | NotQuoted
         | InQuotes

    let (|Quote|NotQuote|) c = 
        if c = '"' then Quote else NotQuote

    let checkDelimiter (settings:Settings) (stream:CharStream) (matchedChar:char):DelimiterType = 
        match matchedChar with
        | c when c = settings.ColDelimiterStart -> 
            match settings.ColumnComparer stream c with
            | true,chars -> Column chars
            | false,chars -> NoMatch chars
        | c when (c = '\r' || c = '\n') && settings.RowIsNewLine -> 
            match settings.RowComparer stream c with
            | true,chars -> Row chars
            | false,chars -> NoMatch chars                 
        | c when c = settings.RowDelimiterStart -> 
            match settings.RowComparer stream c with
            | true,chars -> Row chars
            | false,chars -> NoMatch chars
        | _ -> NoMatch [|matchedChar|]
        
            
    
    let getField (settings:Settings) charStream = 
        let validField = string >> Valid >> EndOfField
        let validRow = string >> Valid >> EndOfRow
        let validFile = string >> Valid >> EndOfFile

        let invalidField = Invalid |> EndOfField
        let invalidRow = Invalid |> EndOfRow
        let invalidFile = Invalid |> EndOfFile

        let rec loopQuote quoteState fieldPosition fieldLength (stream:CharStream) (field:StringBuilder) = 
            settings.IsFieldSizeOk fieldLength
            match quoteState,stream.MoveNext() with
            | Ignore,false -> invalidFile
            | _,false -> field |> validFile
            | Ignore,true ->
                match checkDelimiter settings stream stream.Current with
                | Row _ -> invalidRow
                | Column _ -> invalidField
                | _ -> loopQuote Ignore InField (fieldLength + 1) stream field
            | IgnoreQuote,true ->
                match stream.Current with
                | Quote ->
                        match stream.MoveNext(),checkDelimiter settings stream stream.Current with
                        | false,_ -> invalidFile
                        | _,Row _ -> invalidRow
                        | _,Column _ -> invalidField
                        | _,_ -> loopQuote IgnoreQuote InField (fieldLength + 1) stream field
                | _ -> match checkDelimiter settings stream stream.Current with
                       | Row _ -> invalidRow                       
                       | _ -> loopQuote IgnoreQuote InField (fieldLength + 1) stream field
            | _ ->
                match quoteState,fieldPosition,stream.Current with
                | NotQuoted,StartOfField,Quote -> loopQuote InQuotes InField (fieldLength + 1) stream field 
                | NotQuoted,InField,Quote -> loopQuote Ignore InField (fieldLength + 1) stream field
                | InQuotes,InField,Quote ->
                    match stream.MoveNext() with
                    | false -> field |> validFile
                    | _ ->
                        match stream.Current with
                        | Quote -> loopQuote InQuotes InField (fieldLength + 1) stream (field.Append('"'))
                        | _ -> match checkDelimiter settings stream stream.Current with
                               | NoMatch _ -> loopQuote IgnoreQuote InField (fieldLength + 1) stream field
                               | Row cs -> 
                                    stream.Requeue cs
                                    field |> validRow
                               | Column cs ->
                                    stream.Requeue cs
                                    field |> validField
                | InQuotes,InField,c -> loopQuote InQuotes InField (fieldLength + 1) stream (field.Append(c))
                | _ -> 
                    match checkDelimiter settings stream stream.Current with
                    | Column cs ->
                        stream.Requeue cs
                        field |> validField
                    | Row cs ->
                        stream.Requeue cs
                        field |> validRow
                    | NoMatch cs -> loopQuote quoteState InField (fieldLength + 1)  stream (field.Append(cs))
        

        let rec loopField fieldPos fieldLength (stream:CharStream) (field:StringBuilder) = 
            settings.IsFieldSizeOk fieldLength
            match stream.MoveNext() with
            | false -> string field |> Valid |> EndOfFile
            | _ ->
                let c = stream.Current
                match checkDelimiter settings stream c with
                | Column cs -> 
                    stream.Requeue cs
                    field |> validField
                | Row cs -> 
                    stream.Requeue cs
                    field |> validRow
                | NoMatch cs -> loopField InField (fieldLength + 1) stream (field.Append(cs))
                           
        let looper = if settings.HonorQuotes then (loopQuote FieldQuoteState.NotQuoted) else loopField
        looper StartOfField 0 charStream (StringBuilder())