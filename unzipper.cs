using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Compression;


// compilation 

// namespace declaration
namespace UnzipperApp {

public class RecursiveFileSearch
{
    static System.Collections.Specialized.StringCollection log = new System.Collections.Specialized.StringCollection();

    static void Main(string[] Args)
    {
		Arguments args=new Arguments(Args);
		bool overwrite = false;


		if (args["directory"] != null)
		{
			if(args["overwrite"] != null) {
				overwrite = args.IsTrue("overwrite");
			}
			log.Add("start value: "+args.Single("directory"));
			log.Add("overwrite value: "+args.IsTrue("overwrite"));

			WalkDirectoryTree(new DirectoryInfo(args.Single("directory")), overwrite);
		}
		else {
			Console.WriteLine("Unzipper Utility by Roy Cyril Dosado rcdosado(at)gmail.com ");
			Console.WriteLine("--overwrite :: include if you want to override existing extracted directory");
			Console.WriteLine("--directory:<path> :: specify the starting directory");

			Console.WriteLine("Example:  ");
			Console.WriteLine(@"Unzipper.exe --overwrite  --directory:c:\starting-dir\ ");
			Console.WriteLine(@"Unzipper.exe --directory:c:\starting-dir\ ");
		}

    }

    static void WalkDirectoryTree(System.IO.DirectoryInfo root, bool overwrite=false)
    {
        System.IO.FileInfo[] files = null;
        System.IO.DirectoryInfo[] subDirs = null;

        // First, process all the files directly under this folder
        try
        {
            files = root.GetFiles("*.zip");
        }
        // This is thrown if even one of the files requires permissions greater
        // than the application provides.
        catch (UnauthorizedAccessException e)
        {
            // This code just writes out the message and continues to recurse.
            // You may decide to do something different here. For example, you
            // can try to elevate your privileges and access the file again.
            log.Add(e.Message);
        }

        catch (System.IO.DirectoryNotFoundException e)
        {
            Console.WriteLine(e.Message);
        }

        if (files != null)
        {
            foreach (System.IO.FileInfo fi in files)
            {
                // In this example, we only access the existing FileInfo object. If we
                // want to open, delete or modify the file, then
                // a try-catch block is required here to handle the case
                // where the file has been deleted since the call to TraverseTree().

				string zipFolder =  System.IO.Path.GetFileNameWithoutExtension(fi.FullName);
				string parentZipFolder = System.IO.Path.GetDirectoryName(fi.FullName);

				log.Add("zipFolder : "+zipFolder);
				log.Add("parentZipFolder : "+parentZipFolder);

				string fullPath = System.IO.Path.Combine(parentZipFolder, zipFolder);
				log.Add("Full path : "+ fullPath);

				if(!Directory.Exists(fullPath)){

						DirectoryInfo dir_ext = Directory.CreateDirectory(fullPath);
						ZipFile.ExtractToDirectory(fi.FullName, fullPath);
						
				}else {
					if (overwrite)
					{
						log.Add("overwriting directory");
						DirectoryInfo dir = new DirectoryInfo(fullPath);

						log.Add("Removing Readonly Attributes");
						dir.Attributes = dir.Attributes & ~FileAttributes.ReadOnly;

						log.Add("Removing Directory : " + fullPath);
						dir.Delete(true);

						log.Add("Creating New Directory : " + fullPath);
						DirectoryInfo dir_ext = Directory.CreateDirectory(fullPath);

						log.Add("Extracing Zip contents here : " + fullPath);
						ZipFile.ExtractToDirectory(fi.FullName, fullPath);

					} 
					else 
					{
						log.Add(fullPath+" - Skipped..");
						Console.WriteLine("Skipped..");
					}
				}


            }

            // Now find all the subdirectories under this directory.
            subDirs = root.GetDirectories();

            foreach (System.IO.DirectoryInfo dirInfo in subDirs)
            {
                // Resursive call for each subdirectory.
                WalkDirectoryTree(dirInfo);
            }
        }
    }
}

// NOTE --------------------------------------------------------------
// This class is from http://jake.ginnivan.net/c-sharp-argument-parser/
// NOTE --------------------------------------------------------------

/// <summary>
/// Arguments class
/// </summary>
public class Arguments
{ 
    /// <summary>
    /// Splits the command line. When main(string[] args) is used escaped quotes (ie a path "c:\folder\")
    /// Will consume all the following command line arguments as the one argument. 
    /// This function ignores escaped quotes making handling paths much easier.
    /// </summary>
    /// <param name="commandLine">The command line.</param>
    /// <returns></returns>
    public static string[] SplitCommandLine(string commandLine)
    {
        var translatedArguments = new StringBuilder(commandLine);
        var escaped = false;
        for (var i = 0; i < translatedArguments.Length; i++)
        {
            if (translatedArguments[i] == '"')
            {
                escaped = !escaped;
            }
            if (translatedArguments[i] == ' ' && !escaped)
            {
                translatedArguments[i] = '\n';
            }
        }

        var toReturn = translatedArguments.ToString().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < toReturn.Length; i++)
        {
            toReturn[i] = RemoveMatchingQuotes(toReturn[i]);
        }
        return toReturn;
    }

    public static string RemoveMatchingQuotes(string stringToTrim)
    {
        var firstQuoteIndex = stringToTrim.IndexOf('"');
        var lastQuoteIndex = stringToTrim.LastIndexOf('"');
        while (firstQuoteIndex != lastQuoteIndex)
        {
            stringToTrim = stringToTrim.Remove(firstQuoteIndex, 1);
            stringToTrim = stringToTrim.Remove(lastQuoteIndex - 1, 1); //-1 because we've shifted the indicies left by one
            firstQuoteIndex = stringToTrim.IndexOf('"');
            lastQuoteIndex = stringToTrim.LastIndexOf('"');
        }

        return stringToTrim;
    }

    private readonly Dictionary<string, Collection<string>> _parameters;
    private string _waitingParameter;

    public Arguments(IEnumerable<string> arguments)
    {
        _parameters = new Dictionary<string, Collection<string>>();

        string[] parts;

        //Splits on beginning of arguments ( - and -- and / )
        //And on assignment operators ( = and : )
        var argumentSplitter = new Regex(@"^-{1,2}|^/|=|:",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        foreach (var argument in arguments)
        {
            parts = argumentSplitter.Split(argument, 3);
            switch (parts.Length)
            {
                case 1:
                    AddValueToWaitingArgument(parts[0]);
                    break;
                case 2:
                    AddWaitingArgumentAsFlag();

                    //Because of the split index 0 will be a empty string
                    _waitingParameter = parts[1];
                    break;
                case 3:
                    AddWaitingArgumentAsFlag();

                    //Because of the split index 0 will be a empty string
                    string valuesWithoutQuotes = RemoveMatchingQuotes(parts[2]);

                    AddListValues(parts[1], valuesWithoutQuotes.Split(','));
                    break;
            }
        }

        AddWaitingArgumentAsFlag();
    }

    private void AddListValues(string argument, IEnumerable<string> values)
    {
        foreach (var listValue in values)
        {
            Add(argument, listValue);
        }
    }

    private void AddWaitingArgumentAsFlag()
    {
        if (_waitingParameter == null) return;

        AddSingle(_waitingParameter, "true");
        _waitingParameter = null;
    }

    private void AddValueToWaitingArgument(string value)
    {
        if (_waitingParameter == null) return;

        value = RemoveMatchingQuotes(value);

        Add(_waitingParameter, value);
        _waitingParameter = null;
    }

    /// <summary>
    /// Gets the count.
    /// </summary>
    /// <value>The count.</value>
    public int Count
    {
        get
        {
            return _parameters.Count;
        }
    }

    /// <summary>
    /// Adds the specified argument.
    /// </summary>
    /// <param name="argument">The argument.</param>
    /// <param name="value">The value.</param>
    public void Add(string argument, string value)
    {
        if (!_parameters.ContainsKey(argument))
            _parameters.Add(argument, new Collection<string>());

        _parameters[argument].Add(value);
    }

    public void AddSingle(string argument, string value)
    {
        if (!_parameters.ContainsKey(argument))
            _parameters.Add(argument, new Collection<string>());
        else
            throw new ArgumentException(string.Format("Argument {0} has already been defined", argument));

        _parameters[argument].Add(value);
    }

    public void Remove(string argument)
    {
        if (_parameters.ContainsKey(argument))
            _parameters.Remove(argument);
    }

    /// <summary>
    /// Determines whether the specified argument is true.
    /// </summary>
    /// <param name="argument">The argument.</param>
    /// <returns>
    ///     <c>true</c> if the specified argument is true; otherwise, <c>false</c>.
    /// </returns>
    public bool IsTrue(string argument)
    {
        AssertSingle(argument);

        var arg = this[argument];

        return arg != null && arg[0].Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    private void AssertSingle(string argument)
    {
        if (this[argument] != null && this[argument].Count > 1)
            throw new ArgumentException(string.Format("{0} has been specified more than once, expecting single value", argument));
    }

    public string Single(string argument)
    {
        AssertSingle(argument);

        //only return value if its NOT true, there is only a single item for that argument
        //and the argument is defined
        if (this[argument] != null && !IsTrue(argument))
            return this[argument][0];

        return null;
    }

    public bool Exists(string argument)
    {
        return (this[argument] != null && this[argument].Count > 0);
    }

    /// <summary>
    /// Gets the <see cref="System.Collections.ObjectModel.Collection&lt;T&gt;"/> with the specified parameter.
    /// </summary>
    /// <value></value>
    public Collection<string> this[string parameter]
    {
        get
        {
            return _parameters.ContainsKey(parameter) ? _parameters[parameter] : null;
        }
    }
}

} // end namespace