namespace Moiety

    open System
    open Parser
    open System.Collections.Generic

    module Util = 
        let to_option x =
            match x with
                | null -> None
                | _ -> Some(x)

    [<AbstractClass>]
    type DSVParser(chars:charSequence,fieldDelimiter:string,rowDelimiter:string,honorQuotes:bool) = 
        let mutable endOfRow = false
        let mutable endOfFile = false
        let mutable currentRow:string seq = null
        let settings = new ParseSettings (fieldDelimiter,rowDelimiter,honorQuotes)

        member internal x.Chars = chars

        member x.IsEndOfRow = endOfRow
        member x.IsEndOfFile = endOfFile

        member x.Reset() = 
            endOfRow <- false
            endOfFile <- false
            chars.Reset()

        member x.NextField():string =
            if endOfFile then
                null
            else
                let result = getField chars (settings)
                match result with 
                    | (Parser.EndOfFile,field) -> 
                        endOfFile <- true
                        endOfRow <- true
                        field
                    | (Parser.EndOfRow,field) ->
                        endOfRow <- true
                        field
                    | (Parser.EndOfField, field) -> 
                        endOfRow <- false
                        endOfFile <- false
                        field

        member x.CurrentRow = currentRow
        member x.GetNextRow():bool =
            if endOfFile then
                false
            else
                let rec getRow fields = 
                    let field = x.NextField()
                    if endOfRow && fields |> List.length > 0 then
                        (field :: fields) |> List.rev
                    else
                        if endOfFile && fields |> List.length = 0 then
                            []
                        else
                            getRow (field :: fields)
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

        new(stream) = new DSVStream(stream,",",System.Environment.NewLine,true,null)
        new(stream,fieldDelimiter) = new DSVStream(stream,fieldDelimiter,System.Environment.NewLine,true,null)
        new(stream,fieldDelimiter,rowDelimiter) = new DSVStream(stream,fieldDelimiter,rowDelimiter,true,null)
        new(stream,fieldDelimiter,rowDelimiter,honorQuotes) = new DSVStream(stream,fieldDelimiter,rowDelimiter,honorQuotes,null)

    type DSVFile(path:string,fieldDelimiter:string,rowDelimiter:string,honorQuotes:bool,encoding:System.Text.Encoding) =
        inherit DSVParser(new charSequence(new System.IO.FileStream(path,System.IO.FileMode.Open,System.IO.FileAccess.Read),encoding |> Util.to_option),fieldDelimiter,rowDelimiter,honorQuotes)

        new(path) = new DSVFile(path,",",System.Environment.NewLine,true,null)
        new(path,fieldDelimiter) = new DSVFile(path,fieldDelimiter,System.Environment.NewLine,true,null)
        new(path,fieldDelimiter,rowDelimiter) = new DSVFile(path,fieldDelimiter,rowDelimiter,true,null)
        new(path,fieldDelimiter,rowDelimiter,honorQuotes) = new DSVFile(path,fieldDelimiter,rowDelimiter,honorQuotes,null)

        interface IDisposable with
            member x.Dispose() = (x.Chars :> IDisposable).Dispose()        

            

