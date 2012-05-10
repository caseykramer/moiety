﻿namespace Moiety

module Parser = 

    open System

    type ParseSettings(fieldDelim:string,rowDelim:string,honorQuotes:bool) = 
        let fieldLast = fieldDelim.Chars(fieldDelim.Length - 1)
        let rowLast = rowDelim.Chars(rowDelim.Length - 1)
        let rowFirstChar = rowDelim.Chars(0)
        let rowDelimChars = (rowDelim.ToCharArray() |> Array.rev)
        let isRowDelimNewline = rowDelim.Contains("\r") || rowDelim.Contains("\n")
        let fieldDelimChars = fieldDelim.ToCharArray()
        let isSingleCharFieldDelim = fieldDelimChars.Length = 1
        let charFieldDelim = fieldDelimChars.[0]
    
        member x.FieldDelimiter:string = fieldDelim
        member x.RowDelimiter:string = rowDelim
        member x.HonorQuotedFields:bool = honorQuotes
        member x.FieldLast = fieldLast
        member x.RowLast = rowLast
        member x.RowFirstChar = rowFirstChar
        member x.RowDelimChars = rowDelimChars
        member x.IsRowDelimNewline = isRowDelimNewline
        member x.FieldDelimChars = fieldDelimChars
        member x.IsSingleCharFieldDelim = isSingleCharFieldDelim
        member x.CharFieldDelim = fieldDelimChars.[0]


    let defaultSettings = new ParseSettings(",",System.Environment.NewLine,true)

    type ParseState(stream:System.Collections.Generic.IEnumerator<char>,settings:ParseSettings,field:char list,currentChar:char,lastChar:char,isInQuotes:bool, delimMatcher:char->ParseState->CharMatch)= 
        let mutable myStream = stream
        let mutable myField = field
        let mutable myCurrentChar = currentChar
        let mutable myLastChar = lastChar
        let mutable myIsInQuotes = isInQuotes
        let stringBuilder = new System.Text.StringBuilder()

        member x.Stream 
            with get() = myStream
            and  set(v) = myStream <- v
 
        member x.Settings = settings
    
        member x.Field = myField 
            //and set(v) = myField <- v
    
        member x.CurrentChar 
            with get() = myCurrentChar 
            and  set(v) = myCurrentChar <- v

        member x.LastChar 
            with get() = myLastChar 
            and  set(v) = myLastChar <- v

        member x.IsInQuotes 
            with get() = myIsInQuotes 
            and  set(v) = myIsInQuotes <- v
    
        member x.DelimMatcher = delimMatcher

        member x.FieldString = stringBuilder.ToString()

        member x.AddCharacter c = 
            myField <- c :: myField
            stringBuilder.Append(c) |> ignore
    and CharMatch =
        | NoMatch
        | Field
        | Row
        | Skip

    type FieldBounrdy =
        | EndOfField
        | EndOfRow
        | EndOfFile

    type charSequence(stream:System.IO.Stream,encoding:System.Text.Encoding option) =
        let buffersize = 0x80000
        let reader = 
            match encoding with
                | None -> new System.IO.StreamReader(stream,true)
                | Some(e) -> new System.IO.StreamReader(stream,e)
        let buffer = Array.init(buffersize)(fun i -> char(0))
    
        let mutable currentIdx = 0
        let mutable maxIdx = 0

        let reloadBuffer size=
            maxIdx <- reader.Read(buffer,0,size)
            if maxIdx > 0 then
                currentIdx <- 0
                true
            else
                false

        member e.Peek = 
            if currentIdx + 1 = maxIdx then
                if reloadBuffer buffersize then
                    buffer.[currentIdx + 1]
                else
                    failwith "Reached end of file"
            else
                buffer.[currentIdx + 1]
        
        interface System.Collections.Generic.IEnumerator<char> with
            member e.MoveNext() = 
                    let nextIdx = currentIdx + 1
                    if (nextIdx) < maxIdx then
                        currentIdx <- nextIdx
                        true
                    else
                        reloadBuffer buffersize

            member e.Dispose() = reader.Dispose()
            member e.Reset() = 
                currentIdx <- 0
                maxIdx <- 0
            member e.Current with get() = buffer.[currentIdx]
            member e.Current with get() = box(buffer.[currentIdx])    


    let getField (stream) (settings:ParseSettings) =     
        let matchesNewLine (c:char) = 
            match c with 
               | '\r' -> true
               | '\n' -> true
               | _ -> false

        let rec segmentMatches (list1:char list) (list2:char array) = 
            let rec comp (l1:char list) (l2:char array) (idx:int) (max:int) = 
                let next = idx + 1
                match next with 
                    | x when x = max -> l1.[idx] = l2.[idx]
                    | _ ->
                        if l1.[idx] = l2.[idx] then
                            comp l1 l2 next max
                        else
                            false
                
            let len = list2.Length
            if list1.Length < len then
                false
            elif len = 1 then
                list1.Head = list2.[0]
            else
                //(list1 |> Seq.take len |> Seq.toArray) = list2
                (comp (list1) (list2) 0 len)

        let delimiterMatcher = 
            if settings.IsSingleCharFieldDelim && settings.IsRowDelimNewline then
                fun (c:char) (s:ParseState) ->
                    match c with
                        | x when x = settings.CharFieldDelim -> Field
                        | '\r' when s.CurrentChar <> char(0) -> Row
                        | '\n' when s.CurrentChar <> char(0) -> Row
                        | '\r' when s.CurrentChar = char(0)  -> Skip
                        | '\n' when s.CurrentChar = char(0)  -> Skip
                        | _ -> NoMatch
            elif settings.IsSingleCharFieldDelim then
                fun (c:char) (s:ParseState) ->
                    match c with
                        | x when x = settings.CharFieldDelim -> Field
                        | _ when segmentMatches (c :: s.Field) (settings.RowDelimChars) -> Row
                        | _ -> NoMatch
            else
                fun (c:char) (s:ParseState) -> 
                    match c with
                        | x when x = settings.FieldLast && 
                                segmentMatches (c :: s.Field) (settings.FieldDelimChars) -> Field
                        | x when x = settings.RowLast ->
                            match (settings.RowFirstChar) with
                                | _ when segmentMatches (c :: s.Field) (settings.RowDelimChars) -> Row
                                | _ -> NoMatch
                        | _ -> NoMatch


        let cleanDelimiter (field:string) (delimiter:string) =
            match (delimiter.Chars(0)) with
                | '\r' -> field.TrimEnd([|'\r';'\n'|])
                | '\n' -> field.TrimEnd([|'\r';'\n'|])
                | _ -> field.TrimEnd(settings.FieldDelimChars)

        let rec loopFieldWithoutQuotes (s:ParseState) = 
            if s.Stream.MoveNext() then
                let head = s.Stream.Current
                match s.DelimMatcher head s with
                    | Row -> 
                        let field = cleanDelimiter (new String(s.Field |> List.rev |> List.toArray)) s.Settings.RowDelimiter
                        (EndOfRow,field)
                    | Field ->
                        let field = cleanDelimiter (new String(s.Field |> List.rev |> List.toArray)) s.Settings.FieldDelimiter
                        (EndOfField,field)
                    | Skip ->
                        loopFieldWithoutQuotes s
                    | NoMatch -> 
                        s.AddCharacter head
                        s.CurrentChar <- head
                        s.LastChar <- s.CurrentChar
                        loopFieldWithoutQuotes s
            else
                (EndOfFile,new String(s.Field |> List.rev |> List.toArray))

        let rec loopFieldWithQuotes (s:ParseState) =
            if s.Stream.MoveNext() then
                let head = s.Stream.Current
                match s.IsInQuotes with
                    | true -> 
                        match head with
                            | '"' -> 
                                s.IsInQuotes <- false
                                s.CurrentChar <- '"'
                                loopFieldWithQuotes  s
                            | _ ->
                                s.AddCharacter head
                                s.CurrentChar <- head
                                s.LastChar <- s.CurrentChar 
                                loopFieldWithQuotes s
                    | false ->
                        match head with
                            | '"' -> 
                                s.IsInQuotes <- true
                                if s.CurrentChar.Equals('"') then
                                    s.AddCharacter '"'
                                loopFieldWithQuotes s
                            | _ -> 
                                match s.DelimMatcher head s with
                                    | Row -> 
                                        let field = s.FieldString.TrimEnd(s.Settings.RowDelimChars)
                                        (EndOfRow,field)
                                    | Field ->
                                        let field = s.FieldString.TrimEnd(s.Settings.FieldDelimChars)
                                        (EndOfField,field)
                                    | Skip ->
                                        loopFieldWithQuotes s
                                    | NoMatch -> 
                                        s.AddCharacter head
                                        s.CurrentChar <- head
                                        s.LastChar <- s.CurrentChar
                                        loopFieldWithQuotes s
            else
                (EndOfFile,s.FieldString.TrimEnd(s.Settings.RowDelimChars))    

        if settings.HonorQuotedFields then
            loopFieldWithQuotes (new ParseState(stream,settings,[],char(0),char(0),false, delimiterMatcher)) 
        else
            loopFieldWithoutQuotes (new ParseState(stream,settings,[],char(0),char(0),false,delimiterMatcher)) 

    let getFields (stream) settings = 
            seq{
                let run = ref true

                while(!run) do
                    let field = getField stream settings
                    match field with
                        | (EndOfField,field) -> 
                            yield field
                        | (_, field) -> 
                            run := false
                            yield field
            }

    let getRows (stream) settings = 
        let endOfFile = ref false;
        let rec getRow stream settings fields = 
            let field = getField stream settings
            match field with
                | (EndOfField,f) -> getRow stream settings (f :: fields)
                | (EndOfRow,f) -> (f :: fields) |> List.rev
                | (EndOfFile,f) ->
                    endOfFile := true
                    (f :: fields) |> List.rev

        seq{
            while(not !endOfFile) do
                yield getRow stream settings []
        }