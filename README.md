moiety
======

Parser for string delimited files

*moi-e-ty* [moi-i-tee] - _noun_  
   _an indefinite portion, part, or share_

#### Features
* Supports any string as a field delimiter (which can include multiple characters)
* Supports any string as a row delimiter (again, supporting multiple characters)
* Supports quoted fields
* Handles newlines in a sensable fashion (will support CR+LF,CR, or LF if row delimiters contains a CR or LF)
* Supports file encoding detection using BOM
* Parses at a rate of around 10MB/sec (* on my machine, with standard comma/newline delimiters)
* Provides constant memory usage, even when dealing with large files

### How to use it
There are two classes available depdending on whether you're working with files or generic streams.  There 
are constructor overloads on each that allow you to specify field and row delimiters, and whether or not to
honor quoted fields.  In addition to these you can also specify the encoding when working with files (if you
don't we use StreamReader to detect the encoding based on BOM).

Samples:

F#:
```fsharp

module MyThingThatNeedsCSVData

open Moiety

use parser = new DSVFile("myfile.csv") // This assumes field delimiter = , row delimiter = \r\n

// Now you can get data one field at a time
let field = parser.NextField()

// Or, you can get data one row at a time
let row = parser.NextRow()
```

C#
```csharp

using Moiety

namespace MyThingThatUsesCSV
{
	public class MyCSVUsingClass : IDisposable
	{

		private readonly DSVFile _parser
		public MyCSVUsingClass(string fileName)
		{
			_parser = new DSVFile(fileName); // This assumes field delimiter = , row delimiter = \r\n
		}

		public string GetField()
		{
			return _parser.NextField();
		}

		public IEnumerable<string> GetRow()
		{
			yield return _parser.NextRow();
		}

		// We need to dispose the parser, since it handles creating the file stream
		public void Dispose()
		{
			_parser.Dispose();
		}
	}
}
```


##### Why I wrote my own
Basically I needed a parser at work, and the existing solutions in .Net were either not licensed
in a way I could use, or didn't have support for variable-length field and row delimiters

##### Why not use FParsec?
I tried, and I was actually pleased with the way the grammer came together.  The problem was that when
I was parsing large files, memory was constantly going up.  I think this has to do with the state
information that is kept during the parsing, but I'm not 100% sure.  I did as much tweaking as I could,
but I was never able to get something that had a flat memory usage profile.
