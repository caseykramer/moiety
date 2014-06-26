namespace Moiety

    open System
    open Parser
    open System.Collections.Generic

    module private Util = 
        let to_option x =
            match x with
                | null -> None
                | _ -> Some(x)

    [<AbstractClass>]
    type DSVParser(chars:CharStream,fieldDelimiter:string,rowDelimiter:string,honorQuotes:bool) = 
        let mutable endOfRow = false
        let mutable endOfFile = false
        let mutable currentRow:string seq = null
        let mutable settings = new Settings (fieldDelimiter,rowDelimiter,honorQuotes,None)
        //let delimiter = getDelimiterMatcher settings
        //let mutable parseState = new ParseState(chars,settings,[],char(0),char(0),false, delimiter)
        let mutable rowError = false
        
        member internal x.Chars = chars

        member x.RowError = rowError
        member x.CurrentEncoding with get() = chars.CurrentEncoding
        member x.IsEndOfRow = endOfRow
        member x.IsEndOfFile = endOfFile
        member x.MaxFieldSize
            with get() = match settings.FieldMaxSize with
                         | None -> -1
                         | Some i -> i
            and set(v) = if v <= 0 
                            then settings <- new Settings (fieldDelimiter,rowDelimiter,honorQuotes,None)
                            else settings <-new Settings (fieldDelimiter,rowDelimiter,honorQuotes,Some v)
                         x.Reset()


        member x.Reset() = 
            endOfRow <- false
            endOfFile <- false
            chars.Reset()
            currentRow <- null
            //parseState <- new ParseState(chars,settings,[],char(0),char(0),false,delimiter)

        member x.NextField():string =
            if endOfFile then
                null
            else
                let result = getField (settings) chars 
                match result with 
                    | EndOfFile (Valid field) -> 
                        endOfFile <- true
                        endOfRow <- true
                        field
                    | EndOfFile (Invalid) ->
                        endOfFile <- true
                        endOfRow <- true
                        rowError <- true
                        null
                    | EndOfRow (Valid field) ->                        
                        endOfRow <- true
                        field
                    | EndOfRow (Invalid) ->
                        endOfRow <- true
                        rowError <- true
                        null
                    | EndOfField(Valid field) -> 
                        endOfRow <- false
                        endOfFile <- false
                        field
                    | EndOfField(Invalid) ->
                        endOfRow <- false
                        endOfFile <- false
                        rowError <- true
                        null                    

        member x.CurrentRow = currentRow
        member x.GetNextRow():bool =
            if endOfFile then
                false
            else
                rowError <- false
                let rec getRow fields = 
                    let field = x.NextField()
                    if endOfRow && not endOfFile then
                        match field with
                        | null -> None::fields |> List.rev
                        | _ -> Some field :: fields |> List.rev
                    elif endOfFile && (field.Length > 0 || fields.Length > 0) then
                        match field with
                        | null -> None::fields |> List.rev
                        | _ -> Some field :: fields |> List.rev
                    elif endOfFile && fields |> List.length = 0 then
                            []
                    else 
                        match field with
                        | null -> getRow (None::fields)
                        | _ -> getRow (Some field::fields)
                match getRow [] with
                | [] ->
                    false
                | _ as row ->
                    currentRow <- row |> Seq.choose id
                    true
                                
        member x.AllRows() = 
            seq{
                while x.GetNextRow() do
                    yield x.CurrentRow
            }
            

    type DSVStream(stream:System.IO.Stream,fieldDelimiter:string,rowDelimiter:string,honorQuotes:bool,encoding:System.Text.Encoding) =        
        inherit DSVParser(new CharStream (stream,encoding |> Util.to_option),fieldDelimiter,rowDelimiter,honorQuotes)

        new(stream) = new DSVStream(stream,Parser.defaultSettings.ColumnDelimiter,Parser.defaultSettings.RowDelimiter,Parser.defaultSettings.HonorQuotes,null) 
        new(stream,fieldDelimiter) = new DSVStream(stream,fieldDelimiter,Parser.defaultSettings.RowDelimiter,Parser.defaultSettings.HonorQuotes,null)
        new(stream,fieldDelimiter,rowDelimiter) = new DSVStream(stream,fieldDelimiter,rowDelimiter,Parser.defaultSettings.HonorQuotes,null)
        new(stream,fieldDelimiter,rowDelimiter,honorQuotes) = new DSVStream(stream,fieldDelimiter,rowDelimiter,honorQuotes,null)
        new(stream,fieldDelimiter,rowDelimiter,honorQuotes,maxFieldSize:int) as this = new DSVStream(stream,fieldDelimiter,rowDelimiter,honorQuotes,null) then this.MaxFieldSize <- maxFieldSize

    type DSVFile(path:string,fieldDelimiter:string,rowDelimiter:string,honorQuotes:bool,encoding:System.Text.Encoding) =
        inherit DSVParser(new CharStream(new System.IO.FileStream(path,System.IO.FileMode.Open,System.IO.FileAccess.Read),encoding |> Util.to_option),fieldDelimiter,rowDelimiter,honorQuotes)

        new(path) = new DSVFile(path,Parser.defaultSettings.ColumnDelimiter,Parser.defaultSettings.RowDelimiter,Parser.defaultSettings.HonorQuotes,null)
        new(path,fieldDelimiter) = new DSVFile(path,fieldDelimiter,Parser.defaultSettings.RowDelimiter,Parser.defaultSettings.HonorQuotes,null)
        new(path,fieldDelimiter,rowDelimiter) = new DSVFile(path,fieldDelimiter,rowDelimiter,Parser.defaultSettings.HonorQuotes,null)
        new(path,fieldDelimiter,rowDelimiter,honorQuotes) = new DSVFile(path,fieldDelimiter,rowDelimiter,honorQuotes,null)
        new(path,fieldDelimiter,rowDelimiter,honorQuotes,maxFieldSize:int) as this = new DSVFile(path,fieldDelimiter,rowDelimiter,honorQuotes) then this.MaxFieldSize <- maxFieldSize

        interface IDisposable with
            member x.Dispose() = (x.Chars :> IDisposable).Dispose()        

            
