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
* Includes an optional field-length limit to keep badly-formed data from eating memory


[![Build status](https://ci.appveyor.com/api/projects/status/591mu9ky254ahkxl)](https://ci.appveyor.com/project/caseykramer/moiety)

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
let field = parser.GetNextField()

// Or, you can get data one row at a time
parser.GetNextRow()
let row = parser.CurrentRow
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
			// you can also set a field-size limit, which raises an exception when a single
			// field grows beyond the limit specified
			_parser.MaxFieldSize = 1000000; // 1 million character limit
		}

		public string GetField()
		{
			return _parser.NextField();
		}

		public IEnumerable<string> GetRow()
		{
			_parser.GetNextRow();
			yield return _parser.CurrentRow;
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
Basically I needed a parser, and the existing solutions in .Net were either not licensed
in a way I could use (MIT or Apache), or didn't have support for variable-length field and row delimiters. 
The initial spikes were to support a need at work, which got canned, so I decided to take a Saturday and build 
something I could share.

##### Why not use FParsec?
I tried, and I was actually pleased with the way the grammer came together.  The problem was that when
I was parsing large files, memory was constantly going up.  I think this has to do with the state
information that is kept during the parsing, but I'm not 100% sure.  I did as much tweaking as I could,
but I was never able to get something that had a flat memory usage profile.

##### Is it production ready?
Yes! Some months after my original Saturday hacking session I was approached by my old team because the production 
issue that prompted this whole thing reared its ugly head again. I mentioned this project, and worked with them
to get it integrated (and fix a few bugs along the way). It has been running in production since Jan. 2013.
