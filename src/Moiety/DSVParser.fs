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

        member x.NextRow() = 
            seq {
                while not endOfRow do
                    yield x.NextField()
            }
                
        member x.AllRows() = 
            seq{
                while(not endOfFile) do
                    yield x.NextRow()
            }
            

    type DSVStream(stream:System.IO.Stream,fieldDelimiter:string,rowDelimiter:string,honorQuotes:bool) =        
        inherit DSVParser(new charSequence (stream,None),fieldDelimiter,rowDelimiter,honorQuotes)

        new(stream) = new DSVStream(stream,",",System.Environment.NewLine,true)
        new(stream,fieldDelimiter) = new DSVStream(stream,fieldDelimiter,System.Environment.NewLine,true)
        new(stream,fieldDelimiter,rowDelimiter) = new DSVStream(stream,fieldDelimiter,rowDelimiter,true)

    type DSVFile(path:string,fieldDelimiter:string,rowDelimiter:string,honorQuotes:bool,encoding:System.Text.Encoding) =
        inherit DSVParser(new charSequence(new System.IO.FileStream(path,System.IO.FileMode.Open,System.IO.FileAccess.Read),encoding |> Util.to_option),fieldDelimiter,rowDelimiter,honorQuotes)

        new(path) = new DSVFile(path,",",System.Environment.NewLine,true,null)
        new(path,fieldDelimiter) = new DSVFile(path,fieldDelimiter,System.Environment.NewLine,true,null)
        new(path,fieldDelimiter,rowDelimiter) = new DSVFile(path,fieldDelimiter,rowDelimiter,true,null)
        new(path,fieldDelimiter,rowDelimiter,honorQuotes) = new DSVFile(path,fieldDelimiter,rowDelimiter,honorQuotes,null)

        interface IDisposable with
            member x.Dispose() = (x.Chars :> IDisposable).Dispose()        

            

