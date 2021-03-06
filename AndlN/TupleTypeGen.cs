﻿/// Andl is A New Data Language. See andl.org.
///
/// Copyright © David M. Bennett 2015-16 as an unpublished work. All rights reserved.
///
/// This software is provided in the hope that it will be useful, but with 
/// absolutely no warranties. You assume all responsibility for its use.
/// 
/// This software is completely free to use for purposes of personal study. 
/// For distribution, modification, commercial use or other purposes you must 
/// comply with the terms of the licence originally supplied with it in 
/// the file Licence.txt or at http://andl.org/Licence/.
///
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Andl.Common;

namespace AndlN {
  ///==========================================================================
  /// <summary>
  /// Templater specialised for generating Thrift IDL
  /// </summary>
  internal class TupleTypeTemplater : Templater {

    readonly Dictionary<string, string> _templatedict = new Dictionary<string, string> {
      { "Preamble",    "// <rootname>\n" +
                      "// Tuple type class file generated by Andl -- do not edit\n\n" +
                      "using System;\n" +
                      "namespace AndlN {\n" +
                      "%indent%public class %rootname% {\n" +
                      "%indent2%public const string Connection = @%connection%;\n" },
      { "Postamble",  "%indent%}\n}\n" },
      { "Tuple",      "%indent%public const string %name%Name = %tablename%;\n" +
                      "%indent%public const string %name%Heading = %heading%;\n" +
                      "%indent%public class %name%Tuple {\n" +
                      "%fields%" + 
                      "%indent%}\n" +
                      "%indent%public static IRelatable<%name%Tuple> %name%Relation() {\n" +
                      "%indent%  return Relatable.From%skind%<%name%Tuple>(Connection, %name%Name, %name%Heading);\n" + 
                      "%indent%}\n\n" },
      { "Field",      "%indent2%public %type% %name%;\n" },
    };

    internal TupleTypeTemplater() {
      _templatedicts.Insert(0, _templatedict);
      RightDelim = '%';
      LeftDelim = '%';
    }
  }

  ///==========================================================================
  /// <summary>
  /// Implement generation of tuple type class file
  /// </summary>
  public class TupleTypeGen {

    static readonly Dictionary<CommonType, string> ToCSharpDict = new Dictionary<CommonType, string> {
      { CommonType.Binary, "byte[]" },
      { CommonType.Bool, "bool" },
      { CommonType.Double, "double" },
      { CommonType.Integer, "int" },
      { CommonType.Number, "decimal" },
      { CommonType.Text, "string" },
      { CommonType.Time, "DateTime" },
    };

    Templater Templater { get { return _templater; } set { _templater = value; } }
    Templater _templater;
    Dictionary<string, string> _basenames = new Dictionary<string, string>();
    Dictionary<string, string> _fieldnames;
    SourceKind _skind;
    string _rootname;
    string _connection;
    int _unique = 0;

    // write an entire class file to the text writer
    public static void Process(TextWriter tw, SourceKind skind, string rootname, string connection, CommonHeading[] headings) {
      var gen = new TupleTypeGen() {
        Templater = new TupleTypeTemplater(),
        _skind = skind,
        _rootname = rootname,
        _connection = connection,
      };
      tw.Write(gen.Preamble());
      foreach (var h in headings) {
        gen._fieldnames = new Dictionary<string, string>();
        tw.Write(gen.Tuple(h.Name, h.Fields));
      }
      tw.Write(gen.Postamble());
    }

    string Preamble() {
      var dict = new Dictionary<string, Func<int, string>> {
        { "indent", (x) => Indent(1) },
        { "indent2", (x) => Indent(2) },
        { "rootname", (x) => _rootname },
        { "connection", (x) => Quoted(_connection) },
      };
      return _templater.Process("Preamble", dict);
    }

    string Postamble() {
      var dict = new Dictionary<string, Func<int, string>> {
        { "indent", (x) => Indent(1) },
      };
      return _templater.Process("Postamble", dict);
    }

    string Tuple(string rawname, CommonField[] columns) {
      var name = Clean(rawname, _basenames);
      var dict = new Dictionary<string, Func<int, string>> {
        { "indent", (x) => Indent(2) },
        { "name", (x) => name },
        { "rootname", (x) => _rootname },
        { "tablename", (x) => Quoted(rawname) },
        { "basename", (x) => $"NdlTuple<{name}>" },
        { "fields", (x) => Fields(columns) },
        { "heading", (x) => Heading(columns) },
        { "skind", (x) => _skind.ToString() },
      };
      return _templater.Process("Tuple", dict);
    }

    string Fields(CommonField[] columns) {
      var dict = new Dictionary<string, Func<int, string>> {
        { "indent2", (x) => Indent(3) },
        { "type", (x) => TypeName(columns[x].CType) },
        { "name", (x) => Clean(columns[x].Name, _fieldnames) },
      };
      return _templater.Process("Field", dict, columns.Length, "");
    }

    string Indent(int x) {
      return new string(' ', x * 2);
    }

    string TypeName(CommonType ctype) {
      return ToCSharpDict[ctype];
    }

    string Heading(CommonField[] fields) {
      var s = fields.Select(f => $"{Clean(f.Name, _fieldnames)}:{f.CType}").Join(",");
      return Quoted(s);
    }

  string Quoted(string s) {
    return $"\"{s}\"";
  }

  string Clean(string name, Dictionary<string, string> dict) {
      if (!dict.ContainsKey(name)) {
        var sb = new StringBuilder(name);
        for (int i = 0; i < sb.Length; ++i) {
          if (i == 0 && !Char.IsLetter(sb[i])) sb[i] = '_';
          else if (!Char.IsLetterOrDigit(sb[i])) sb[i] = '_';
        }
        if (dict.ContainsValue(sb.ToString())) sb.Append("_").Append(++_unique);
        dict.Add(name, sb.ToString());
      }
      return dict[name];
    }
  }
}
