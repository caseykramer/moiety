namespace Moiety.Parser.Tests

open NUnit.Framework
open System.IO
open Moiety.Parser
open FsUnit


module ``Given a parser`` =

    [<Test>]
    let ``When passed a string containing text with no separators it returns it`` () =
        let testString = string("onetwothree")
        use chars = new CharStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testString)),Some(System.Text.Encoding.UTF8))

        let result = getField (defaultSettings) chars  //<| new ParseState(chars,defaultSettings,[],char(0),char(0),false, (getDelimiterMatcher defaultSettings))
        result |> should equal (EndOfFile(Valid "onetwothree"))

    [<Test>]
    let ``When passed a string containing text with a single separator it returns the first field (before the separator)`` ()=
        let testString = string("one,two")
        use chars = new CharStream (new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testString)),Some(System.Text.Encoding.UTF8))
        let result = getField (defaultSettings) chars  // <| new ParseState(chars,defaultSettings,[],char(0),char(0),false, (getDelimiterMatcher defaultSettings))
        result |> should equal (EndOfField(Valid "one"))

    [<Test>]
    let ``When passed a string containing multiple fields it returns the fields as a sequence`` ()=
        let testString = string("one,two")
        let parser = new Moiety.DSVStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testString)))
        parser.GetNextRow() |> should equal true
        let result = parser.CurrentRow |> Seq.toList

        result |> should contain "one"
        result |> should contain "two"

        parser.GetNextRow() |> should equal false

    [<Test>]
    let ``When passed a string containing multiple rows GetNextRow() returns all fields in a single row`` ()=
        let testString = string("one,two\r\nthree,four")
        let parser = new Moiety.DSVStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testString)))
        parser.GetNextRow() |> should equal true
        let result = parser.CurrentRow |> Seq.toList

        result |> Seq.length |> should equal 2
        result |> should contain "one"
        result |> should contain "two"


    [<Test>]
    let ``When passed a string containing multiple rows AllRows returns all rows with all fields in each row as a sequence`` ()=
        let testString = string("one,two\r\nthree,four")
        let parser = new Moiety.DSVStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testString)))
        let result = parser.AllRows() |> List.ofSeq
         
        result.Length |> should equal 2
        let row1 = result.[0] |> List.ofSeq
        row1 |> Seq.length |> should equal 2
        row1 |> should contain "one"
        row1 |> should contain "two"

        let row2 = result.[1] |> List.ofSeq
        row2 |> Seq.length |> should equal 2
        row2 |> should contain "three"
        row2 |> should contain "four" 

    [<Test>]
    let ``When passed a string containing fields which are surrounded by double quotes, the quotes are not included in the result`` () =
        let testString = string("\"one\",\"two\"")
        let parser = new Moiety.DSVStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testString)))
        parser.GetNextRow() |> should equal true
        let result = parser.CurrentRow |> Seq.toList

        result |> Seq.length |> should equal 2
        result |> should contain "one"
        result |> should contain "two"

    [<Test>]
    let ``When passed a string containing a field surrounded by double quotes, that field may contain a field delimiter`` ()=
        let testString = string("\"one,two\",three")
        let parser = new Moiety.DSVStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testString)))
        parser.GetNextRow() |> should equal true
        let result = parser.CurrentRow |> Seq.toList

        result |> Seq.length |> should equal 2
        result |> should contain "one,two"
        result |> should contain "three"

    [<Test>]
    let ``When passed a string containing a field surrounded by double quotes, a double quote character in the field should be escaped with two double quote characters`` ()=
        let testString = string("\"one,\"\"two\"\"\",three")
        let parser = new Moiety.DSVStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testString)))
        parser.GetNextRow() |> should equal true
        let result = parser.CurrentRow |> Seq.toList
        

        result |> Seq.length |> should equal 2
        result |> should contain "one,\"two\""
        result |> should contain "three"

    [<Test>]
    let ``When passed a string containing a field surrounded by double quotes, that field may contain a row delimiter (newline)`` () =
        let testString = string ("\"one\r\ntwo\",three")
        let parser = new Moiety.DSVStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testString)))
        parser.GetNextRow() |> should equal true
        let result = parser.CurrentRow |> Seq.toList

        result |> Seq.length |> should equal 2
        result |> should contain "one\r\ntwo"
        result |> should contain "three"

    [<Test>]
    let ``Can specify an alternate delimiter for fields (such as |)`` ()=
        let testString = string("one|two|three|four\r\na|b|c|d")
        let parser = new Moiety.DSVStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testString)),"|")
        let result = parser.AllRows() |> Seq.toList

        result.Length |> should equal 2
        let row1 = result.[0] |> List.ofSeq
        row1 |> Seq.length |> should equal 4
        row1 |> should contain "one"
        row1 |> should contain "two"
        row1 |> should contain "three"
        row1 |> should contain "four"

        let row2 = result.[1] |> List.ofSeq
        row2 |> Seq.length |> should equal 4
        row2 |> should contain "a"
        row2 |> should contain "b"
        row2 |> should contain "c"
        row2 |> should contain "d" 

    [<Test>]
    let ``Can specify an alternate delimiter for rows (such as ::)`` ()=
        let testString = string("one,two,three,four::a,b,c,d")
        let parser = new Moiety.DSVStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testString)),",","::")
        let result = parser.AllRows() |> List.ofSeq

        result.Length |> should equal 2
        let row1 = result.[0] |> List.ofSeq
        row1 |> Seq.length |> should equal 4
        row1 |> should contain "one"
        row1 |> should contain "two"
        row1 |> should contain "three"
        row1 |> should contain "four"

        let row2 = result.[1]
        row2 |> Seq.length |> should equal 4
        row2 |> should contain "a"
        row2 |> should contain "b"
        row2 |> should contain "c"
        row2 |> should contain "d" 

    [<Test>]
    let ``Delimiters inside double quoted fields are not skipped if HonorQuotedFields is false`` ()=
        let testString = string("\"one,two\",three")
        let parser = new Moiety.DSVStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testString)),defaultSettings.ColumnDelimiter,defaultSettings.RowDelimiter,false)
        parser.GetNextRow() |> should equal true
        let result = parser.CurrentRow |> Seq.toList

        result |> Seq.length |> should equal 3
        result |> should contain "\"one"
        result |> should contain "two\""
        result |> should contain "three"

    [<Test>]
    let ``Fields beginning with quotes that are quoted are parsed correctly`` () =

        let testString = "\"\"\"one\"\",two,three\",four,five"
        let parser = new Moiety.DSVStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testString)))
        parser.GetNextRow() |> should equal true
        let result = parser.CurrentRow |> Seq.toList

        result |> Seq.length |> should equal 3
        result |> should contain "\"one\",two,three"
        result |> should contain "four"
        result |> should contain "five"

    [<Test>]
    let ``Can use a unix-style line ending with quoted fields`` ()=
        let testString = "\"one\",\"two\",\"three\"\n\"a\",\"b\",\"c\""
        let parser = new Moiety.DSVStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testString)),defaultSettings.ColumnDelimiter,"\n")
        let result = parser.AllRows() |> Seq.toList

        result.Length |> should equal 2
        result.[0] |> should contain "one"
        result.[0] |> should contain "two"
        result.[0] |> should contain "three"
        result.[1] |> should contain "a"
        result.[1] |> should contain "b"
        result.[1] |> should contain "c"

    [<Test>]
    let ``Final Row-Delimiters are ignored if there is no row text`` () =
        let testString = "one,two,three\r\nfour,five,six\r\n"
        let parser = new Moiety.DSVStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testString)))
        let result = parser.AllRows() |> Seq.toList

        result.Length |> should equal 2
        result.[0] |> should contain "one"
        result.[0] |> should contain "two"
        result.[0] |> should contain "three"
        result.[1] |> should contain "four"
        result.[1] |> should contain "five"
        result.[1] |> should contain "six"

    [<Test>]
    let ``Reset restores reader to original state so you can parse contents from the beginning again....without breaking this time`` () =
        let testString = "one,two,three\r\n1,2,3\r\n4,5,6\r\n7,8,9"
        let encoding = new System.Text.UnicodeEncoding(true,true)
        let parser = new Moiety.DSVStream(new MemoryStream(System.Text.Encoding.Convert(System.Text.Encoding.UTF8,encoding,System.Text.Encoding.UTF8.GetBytes(testString))),",",System.Environment.NewLine,false,encoding)
        let result = parser.AllRows() |> List.ofSeq

        result.Length |> should equal 4

        parser.Reset()
        parser.CurrentRow |> should equal null
        parser.GetNextRow() |> should equal true
        let firstRow = parser.CurrentRow |> List.ofSeq
        firstRow |> should contain "one"
        firstRow |> should contain "two"
        firstRow |> should contain "three"

    [<Test>]
    let ``Can handle empty fields at the end of rows`` () =
        let testString = "one,two,three\r\n1,2,\r\n3,4,5"
        let parser = new Moiety.DSVStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testString)))
        parser.GetNextRow() |> should equal true
        let result1 = parser.CurrentRow |> List.ofSeq
        result1.Length |> should equal 3

        result1.[0] |> should contain "one"
        result1.[1] |> should contain "two"
        result1.[2] |> should contain "three"

        parser.GetNextRow() |> should equal true
        let result2 = parser.CurrentRow |> List.ofSeq
        result2.Length |> should equal 3
        result2.[0] |> should equal "1"
        result2.[1] |> should equal "2"
        result2.[2] |> should equal ""
        
        parser.GetNextRow() |> should equal true
        let result3 = parser.CurrentRow |> List.ofSeq
        result3.[0] |> should equal "3"
        result3.[1] |> should equal "4"
        result3.[2] |> should equal "5"

        parser.GetNextRow() |> should equal false

    [<Test>]
    let ``Can handle empty fields at the beginning of rows`` () =
        let testString = "one,two,three\r\n,1,2\r\n3,4,5"
        let parser = new Moiety.DSVStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testString)))
        parser.GetNextRow() |> should equal true
        let result1 = parser.CurrentRow |> List.ofSeq

        result1.Length |> should equal 3
        result1 |> should contain "one"
        result1 |> should contain "two"
        result1 |> should contain "three"

        parser.GetNextRow() |> should equal true
        let result2 = parser.CurrentRow |> List.ofSeq
        
        result2.Length |> should equal 3
        result2.[0] |> should equal ""
        result2.[1] |> should equal "1"
        result2.[2] |> should equal "2"

        parser.GetNextRow() |> should equal true
        let result3 = parser.CurrentRow |> List.ofSeq

        result3.Length |> should equal 3
        result3.[0] |> should equal "3"
        result3.[1] |> should equal "4"
        result3.[2] |> should equal "5"

        parser.GetNextRow() |> should equal false

    [<Test>]
    let ``Can handle empty fields in the middle of rows`` () =
        let testString = "one,two,three\r\n1,,2\r\n3,4,5"
        let parser = new Moiety.DSVStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testString)))
        parser.GetNextRow() |> should equal true
        let result1 = parser.CurrentRow |> List.ofSeq

        result1.Length |> should equal 3
        result1 |> should contain "one"
        result1 |> should contain "two"
        result1 |> should contain "three"

        parser.GetNextRow() |> should equal true
        let result2 = parser.CurrentRow |> List.ofSeq
        
        result2.Length |> should equal 3
        result2.[0] |> should equal "1"
        result2.[1] |> should equal ""
        result2.[2] |> should equal "2"

        parser.GetNextRow() |> should equal true
        let result3 = parser.CurrentRow |> List.ofSeq

        result3.Length |> should equal 3
        result3.[0] |> should equal "3"
        result3.[1] |> should equal "4"
        result3.[2] |> should equal "5"

        parser.GetNextRow() |> should equal false

    [<Test>]
    let ``Can handle empty fields in the entire row`` () =
        let testString = "one,two,three\r\n,,\r\n1,2,3"
        let parser = new Moiety.DSVStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testString)))
        parser.GetNextRow() |> should equal true
        let result1 = parser.CurrentRow |> List.ofSeq

        result1.Length |> should equal 3
        result1 |> should contain "one"
        result1 |> should contain "two"
        result1 |> should contain "three"

        parser.GetNextRow() |> should equal true
        let result2 = parser.CurrentRow |> List.ofSeq
        
        result2.Length |> should equal 3
        result2.[0] |> should equal ""
        result2.[1] |> should equal ""
        result2.[2] |> should equal ""

        parser.GetNextRow() |> should equal true
        let result3 = parser.CurrentRow |> List.ofSeq

        result3.Length |> should equal 3
        result3.[0] |> should equal "1"
        result3.[1] |> should equal "2"
        result3.[2] |> should equal "3"

        parser.GetNextRow() |> should equal false

    [<Test>]
    let ``Can handle multiple rows of empty fields`` () =
        let testString = "one,two,three\r\n,,\r\n,,\r\n1,2,3"
        let parser = new Moiety.DSVStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testString)))
        parser.GetNextRow() |> should equal true
        let result1 = parser.CurrentRow |> List.ofSeq

        result1.Length |> should equal 3
        result1 |> should contain "one"
        result1 |> should contain "two"
        result1 |> should contain "three"

        parser.GetNextRow() |> should equal true
        let result2 = parser.CurrentRow |> List.ofSeq
        
        result2.Length |> should equal 3
        result2.[0] |> should equal ""
        result2.[1] |> should equal ""
        result2.[2] |> should equal ""

        parser.GetNextRow() |> should equal true
        let result3 = parser.CurrentRow |> List.ofSeq

        result3.Length |> should equal 3
        result3.[0] |> should equal ""
        result3.[1] |> should equal ""
        result3.[2] |> should equal ""

        parser.GetNextRow() |> should equal true
        let result4 = parser.CurrentRow |> List.ofSeq

        result4.Length |> should equal 3
        result4.[0] |> should equal "1"
        result4.[1] |> should equal "2"
        result4.[2] |> should equal "3"

        parser.GetNextRow() |> should equal false

    [<Test>]
    let ``Can correctly parse single column files (no column delimiter)`` () = 
        let testString = 
            [ "one"
              "two"
              "three"
              "four"
              "five"
              "six" ] |> List.reduce (sprintf "%s\r\n%s")
        let parser = new Moiety.DSVStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testString)))
        let results = parser.AllRows() |> List.ofSeq
        results |> List.length |> should equal 6

    [<Test>]
    let ``Can set field size limit for early bailout of badly formed files`` () = 
        let testString = ",,\r\n,,\r\n1,2,3\r\none,two,three\r\n,,\r\n,,\r\n1,2,3"
        let parser = new Moiety.DSVStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testString)),MaxFieldSize = 2)
        (fun () -> parser.AllRows() |> List.ofSeq |> ignore) |> should throw typeof<System.Exception>

    [<Test>]
    let ``Field size limit correctly resets after each field is parsed`` () = 
        let testString = ",,\r\n1,2,3\r\n1,2,3\r\n1,2,3\r\n1,2,3"
        let parser = new Moiety.DSVStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testString)),MaxFieldSize = 2)
        let rows = parser.AllRows() |> List.ofSeq
        rows |> List.length |> should equal 5

    [<Test>]
    let ``Standard newline endings will allow mixed CR, LR, and CR_LF``() = 
        let testString = "1,2\r\n3,4\r5,6\n7,8\n9,10\r\n11,12\r\n"
        let parser = new Moiety.DSVStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testString)))
        let rows = parser.AllRows() |> List.ofSeq
        rows |> List.length |> should equal 6

    module ``When using Quoted Fields`` =

        let getParser (text:string) = new Moiety.DSVStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(text)))

        [<Test>]
        let ``A single double quote in the middle a non-quoted field will mark the field as invalid, terminating at the field delimiter``() =
            let testString = "123,456,789\r\none,t\"wo,three\r\nfour,\"five\",six"
            let parser = getParser testString
            parser.GetNextRow() |> should be True
            parser.CurrentRow |> should contain "123"
            parser.CurrentRow |> should contain "456"
            parser.CurrentRow |> should contain "789"
            
            parser.GetNextRow() |> should be True
            parser.RowError |> should be True
            parser.CurrentRow |> Seq.length |> should equal 3
            parser.CurrentRow |> should contain "one"
            parser.CurrentRow |> should contain "three"

            parser.GetNextRow() |> should be True
            parser.RowError |> should be False
            parser.CurrentRow |> should contain "four"
            parser.CurrentRow |> should contain "five"
            parser.CurrentRow |> should contain "six"

            parser.GetNextRow() |> should be False

        [<Test>]
        let ``A single double quote in the last field in a row will mark the field invalid, terminating at the row delimiter``() = 
            let teststring = "123,456,789\r\none,two,th\"ree\r\nfour,\"five\",six"
            let parser = getParser teststring
            parser.GetNextRow() |> should be True
            parser.CurrentRow |> should contain "123"
            parser.CurrentRow |> should contain "456"
            parser.CurrentRow |> should contain "789"
            
            parser.GetNextRow() |> should be True
            parser.RowError |> should be True
            parser.CurrentRow |> Seq.length |> should equal 3
            parser.CurrentRow |> should contain "one"
            parser.CurrentRow |> should contain "two"

            parser.GetNextRow() |> should be True
            parser.RowError |> should be False
            parser.CurrentRow |> should contain "four"
            parser.CurrentRow |> should contain "five"
            parser.CurrentRow |> should contain "six"

            parser.GetNextRow() |> should be False

        [<Test>]
        let ``A single double quote in the middle of a field within double quotes will mark the field invalid, terminating at the first double quote and field delimiter``() = 
            let teststring = "123,456,789\r\none,\"t\"wo\",\"three\"\r\nfour,\"five\",six"
            let parser = getParser teststring
            parser.GetNextRow() |> should be True
            parser.CurrentRow |> should contain "123"
            parser.CurrentRow |> should contain "456"
            parser.CurrentRow |> should contain "789"
            
            parser.GetNextRow() |> should be True
            parser.RowError |> should be True
            parser.CurrentRow |> Seq.length |> should equal 3
            parser.CurrentRow |> should contain "one"
            parser.CurrentRow |> should contain "three"

            parser.GetNextRow() |> should be True
            parser.RowError |> should be False
            parser.CurrentRow |> should contain "four"
            parser.CurrentRow |> should contain "five"
            parser.CurrentRow |> should contain "six"

            parser.GetNextRow() |> should be False

        [<Test>]
        let ``A signel double quote in the middle of a field within double quotes will mark the field invalid, terminating at the NEWLINE if no quote and field delimiter are present``() =
            let teststring = "123,456,789\r\none,\"t\"wo,three\r\nfour,\"five\",six"
            let parser = getParser teststring
            parser.GetNextRow() |> should be True
            parser.CurrentRow |> should contain "123"
            parser.CurrentRow |> should contain "456"
            parser.CurrentRow |> should contain "789"
            
            parser.GetNextRow() |> should be True
            parser.RowError |> should be True
            parser.CurrentRow |> Seq.length |> should equal 3
            parser.CurrentRow |> should contain "one"

            parser.GetNextRow() |> should be True
            parser.RowError |> should be False
            parser.CurrentRow |> should contain "four"
            parser.CurrentRow |> should contain "five"
            parser.CurrentRow |> should contain "six"

            parser.GetNextRow() |> should be False