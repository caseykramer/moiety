namespace Moiety.Parser.Tests

open NUnit.Framework
open System.IO
open Moiety.Parser
open FsUnit


[<TestFixture>]
type ``Given a parser`` () =

    [<Test>]
    member test.``When passed a string containing text with no separators it returns it`` () =
        let testString = string("onetwothree")
        use chars = new charSequence (new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testString)),Some(System.Text.Encoding.UTF8))
        let (x,result) = getField chars (defaultSettings)
        result |> should equal "onetwothree"

    [<Test>]
    member test.``When passed a string containing text with a single separator it returns the first field (before the separator)`` ()=
        let testString = string("one,two")
        use chars = new charSequence (new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testString)),Some(System.Text.Encoding.UTF8))
        let (x,result) = getField chars (defaultSettings)
        result |> should equal "one"

    [<Test>]
    member test.``When passed a string containing multiple fields it returns the fields as a sequence`` ()=
        let testString = string("one,two")
        use chars = new charSequence (new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testString)),Some(System.Text.Encoding.UTF8))
        let result = getFields chars (defaultSettings) |> Seq.toList

        result |> should contain "one"
        result |> should contain "two"

    [<Test>]
    member test.``When passed a string containing multiple rows gitFields returns all fields in a single row`` ()=
        let testString = string("one,two\r\nthree,four")
        use chars = new charSequence (new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testString)),Some(System.Text.Encoding.UTF8))
        let result = getFields chars (defaultSettings) |> Seq.toList

        result |> Seq.length |> should equal 2
        result |> should contain "one"
        result |> should contain "two"


    [<Test>]
    member test.``When passed a string containing multiple rows getRows returns all rows with all fields in each row as a sequence`` ()=
        let testString = string("one,two\r\nthree,four")
        use chars = new charSequence (new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testString)),Some(System.Text.Encoding.UTF8))
        let result = getRows (chars) (defaultSettings) |> Seq.toList

        result.Length |> should equal 2
        let row1 = result.[0]
        row1 |> Seq.length |> should equal 2
        row1 |> should contain "one"
        row1 |> should contain "two"

        let row2 = result.[1]
        row2 |> Seq.length |> should equal 2
        row2 |> should contain "three"
        row2 |> should contain "four" 

    [<Test>]
    member test.``When passed a string containing fields which are surrounded by double quotes, the quotes are not included in the result`` () =
        let testString = string("\"one\",\"two\"")
        use chars = new charSequence (new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testString)),Some(System.Text.Encoding.UTF8))
        let result = getFields chars (defaultSettings) |> Seq.toList

        result |> Seq.length |> should equal 2
        result |> should contain "one"
        result |> should contain "two"

    [<Test>]
    member test.``When passed a string containing a field surrounded by double quotes, that field may contain a field delimiter`` ()=
        let testString = string("\"one,two\",three")
        use chars = new charSequence (new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testString)),Some(System.Text.Encoding.UTF8))
        let result = getFields chars (defaultSettings) |> Seq.toList

        result |> Seq.length |> should equal 2
        result |> should contain "one,two"
        result |> should contain "three"

    [<Test>]
    member test.``When passed a string containing a field surrounded by double quotes, a double quote character in the field should be escaped with two double quote characters`` ()=
        let testString = string("\"one,\"\"two\"\"\",three")
        use chars = new charSequence (new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testString)),Some(System.Text.Encoding.UTF8))
        let result = getFields chars (defaultSettings) |> Seq.toList
        

        result |> Seq.length |> should equal 2
        result |> should contain "one,\"two\""
        result |> should contain "three"

    [<Test>]
    member test.``When passed a string containing a field surrounded by double quotes, that field may contain a row delimiter (newline)`` () =
        let testString = string ("\"one\r\ntwo\",three")
        use chars = new charSequence (new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testString)),Some(System.Text.Encoding.UTF8))
        let result = getFields chars (defaultSettings) |> Seq.toList

        result |> Seq.length |> should equal 2
        result |> should contain "one\r\ntwo"
        result |> should contain "three"

    [<Test>]
    member test.``Can specify an alternate delimiter for fields (such as |)`` ()=
        let testString = string("one|two|three|four\r\na|b|c|d")
        use chars = new charSequence (new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testString)),Some(System.Text.Encoding.UTF8))
        let result = getRows chars (new ParseSettings("|",defaultSettings.RowDelimiter,defaultSettings.HonorQuotedFields)) |> Seq.toList

        result.Length |> should equal 2
        let row1 = result.[0]
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
    member test.``Can specify an alternate delimiter for rows (such as ::)`` ()=
        let testString = string("one,two,three,four::a,b,c,d")
        use chars = new charSequence (new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testString)),Some(System.Text.Encoding.UTF8))
        let result = getRows (chars) (new ParseSettings(defaultSettings.FieldDelimiter,"::",defaultSettings.HonorQuotedFields)) |> Seq.toList

        result.Length |> should equal 2
        let row1 = result.[0]
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
    member test.``Delimiters inside double quoted fields are not skipped if HonorQuotedFields is false`` ()=
        let testString = string("\"one,two\",three")
        use chars = new charSequence (new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testString)),Some(System.Text.Encoding.UTF8))
        let result = getFields chars (new ParseSettings(defaultSettings.FieldDelimiter,defaultSettings.RowDelimiter,false)) |> Seq.toList

        result |> Seq.length |> should equal 3
        result |> should contain "\"one"
        result |> should contain "two\""
        result |> should contain "three"

    [<Test>]
    member test.``Fields beginning with quotes that are quoted are parsed correctly`` () =
        let testString = string("\"\"\"one\"\",two,three\",four,five");
        use chars = new charSequence (new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testString)),Some(System.Text.Encoding.UTF8))
        let result = getFields chars defaultSettings |> Seq.toList

        result |> Seq.length |> should equal 3
        result |> should contain "\"one\",two,three"
        result |> should contain "four"
        result |> should contain "five"

    [<Test>]
    member test.``Can use a unix-style line ending with quoted fields`` ()=
        let testString = "\"one\",\"two\",\"three\"\n\"a\",\"b\",\"c\""
        use chars = new charSequence (new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testString)),Some(System.Text.Encoding.UTF8))
        let result = getRows chars (new ParseSettings(defaultSettings.FieldDelimiter,"\n",true)) |> Seq.toList

        result.Length |> should equal 2
        result.[0] |> should contain "one"
        result.[0] |> should contain "two"
        result.[0] |> should contain "three"
        result.[1] |> should contain "a"
        result.[1] |> should contain "b"
        result.[1] |> should contain "c"
