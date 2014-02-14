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
    type DSVParser(chars:charSequence,fieldDelimiter:string,rowDelimiter:string,honorQuotes:bool) = 
        let mutable endOfRow = false
        let mutable endOfFile = false
        let mutable currentRow:string seq = null
        let mutable settings = new ParseSettings (fieldDelimiter,rowDelimiter,honorQuotes,None)
        let delimiter = getDelimiterMatcher settings
        let mutable parseState = new ParseState(chars,settings,[],char(0),char(0),false, delimiter)                
        
        member internal x.Chars = chars

        member x.IsEndOfRow = endOfRow
        member x.IsEndOfFile = endOfFile
        member x.MaxFieldSize
            with get() = match settings.FieldMaxSize with
                         | None -> -1
                         | Some i -> i
            and set(v) = if v <= 0 
                            then settings <- new ParseSettings (fieldDelimiter,rowDelimiter,honorQuotes,None)
                            else settings <-new ParseSettings (fieldDelimiter,rowDelimiter,honorQuotes,Some v)
                         x.Reset()


        member x.Reset() = 
            endOfRow <- false
            endOfFile <- false
            chars.Reset()
            currentRow <- null
            parseState <- new ParseState(chars,settings,[],char(0),char(0),false,delimiter)

        member x.NextField():string =
            if endOfFile then
                null
            else
                let result = getField chars (settings) parseState
                match result with 
                    | (Parser.EndOfFile,field) -> 
                        endOfFile <- true
                        endOfRow <- true
                        field
                    | (Parser.EndOfRow,field) ->
                        parseState.ResetRow()
                        endOfRow <- true
                        field
                    | (Parser.EndOfField, field) -> 
                        endOfRow <- false
                        endOfFile <- false
                        parseState.ResetField()
                        field

        member x.CurrentRow = currentRow
        member x.GetNextRow():bool =
            if endOfFile then
                false
            else
                let rec getRow fields = 
                    let field = x.NextField()
                    if endOfRow && not endOfFile then
                        (field :: fields) |> List.rev
                    elif endOfFile && (field.Length > 0 || fields.Length > 0) then
                        (field :: fields) |> List.rev
                    elif endOfFile && fields |> List.length = 0 then
                            []
                    else getRow (field :: fields)
                match getRow [] with
                | [] ->
                    false
                | _ as row ->
                    currentRow <- row |> Seq.ofList
                    true
                                
        member x.AllRows() = 
            seq{
                while x.GetNextRow() do
                    yield x.CurrentRow
            }
            

    type DSVStream(stream:System.IO.Stream,fieldDelimiter:string,rowDelimiter:string,honorQuotes:bool,encoding:System.Text.Encoding) =        
        inherit DSVParser(new charSequence (stream,encoding |> Util.to_option),fieldDelimiter,rowDelimiter,honorQuotes)

        new(stream) = new DSVStream(stream,Parser.defaultSettings.FieldDelimiter,Parser.defaultSettings.RowDelimiter,Parser.defaultSettings.HonorQuotedFields,null) 
        new(stream,fieldDelimiter) = new DSVStream(stream,fieldDelimiter,Parser.defaultSettings.RowDelimiter,Parser.defaultSettings.HonorQuotedFields,null)
        new(stream,fieldDelimiter,rowDelimiter) = new DSVStream(stream,fieldDelimiter,rowDelimiter,Parser.defaultSettings.HonorQuotedFields,null)
        new(stream,fieldDelimiter,rowDelimiter,honorQuotes) = new DSVStream(stream,fieldDelimiter,rowDelimiter,honorQuotes,null)
        new(stream,fieldDelimiter,rowDelimiter,honorQuotes,maxFieldSize:int) as this = new DSVStream(stream,fieldDelimiter,rowDelimiter,honorQuotes,null) then this.MaxFieldSize <- maxFieldSize

    type DSVFile(path:string,fieldDelimiter:string,rowDelimiter:string,honorQuotes:bool,encoding:System.Text.Encoding) =
        inherit DSVParser(new charSequence(new System.IO.FileStream(path,System.IO.FileMode.Open,System.IO.FileAccess.Read),encoding |> Util.to_option),fieldDelimiter,rowDelimiter,honorQuotes)

        new(path) = new DSVFile(path,Parser.defaultSettings.FieldDelimiter,Parser.defaultSettings.RowDelimiter,Parser.defaultSettings.HonorQuotedFields,null)
        new(path,fieldDelimiter) = new DSVFile(path,fieldDelimiter,Parser.defaultSettings.RowDelimiter,Parser.defaultSettings.HonorQuotedFields,null)
        new(path,fieldDelimiter,rowDelimiter) = new DSVFile(path,fieldDelimiter,rowDelimiter,Parser.defaultSettings.HonorQuotedFields,null)
        new(path,fieldDelimiter,rowDelimiter,honorQuotes) = new DSVFile(path,fieldDelimiter,rowDelimiter,honorQuotes,null)
        new(path,fieldDelimiter,rowDelimiter,honorQuotes,maxFieldSize:int) as this = new DSVFile(path,fieldDelimiter,rowDelimiter,honorQuotes) then this.MaxFieldSize <- maxFieldSize

        interface IDisposable with
            member x.Dispose() = (x.Chars :> IDisposable).Dispose()        

            

