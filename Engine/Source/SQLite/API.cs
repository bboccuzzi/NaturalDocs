﻿/* 
 * Class: GregValure.NaturalDocs.Engine.SQLite.API
 * ____________________________________________________________________________
 * 
 * A C# interface to selected SQLite API functions.  See <http://www.sqlite.org/capi3ref.html> for descriptions 
 * of the functions and result codes.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details

using System;
using System.Runtime.InteropServices;


#if !SQLITE_UTF8 && !SQLITE_UTF16
	#error You must define either SQLITE_UTF8 or SQLITE_UTF16 to compile Natural Docs.
#endif


namespace GregValure.NaturalDocs.Engine.SQLite
	{
	public static class API
		{
		// Group: Types
		// __________________________________________________________________________


		public enum Result : int
			{
			OK = 0,
			Error = 1,
			Internal = 2,
			Perm = 3,
			Abort = 4,
			Busy = 5,
			Locked = 6,
			NoMem = 7,
			ReadOnly = 8,
			Interrupt = 9,
			IOErr = 10,
			Corrupt = 11,
			NotFound = 12,
			Full = 13,
			CantOpen = 14,
			Protocol = 15,
			Empty = 16,
			Schema = 17,
			TooBig = 18,
			Constraint = 19,
			Mismatch = 20,
			Misuse = 21,
			NoLFS = 22,
			Auth = 23,
			Format = 24,
			Range = 25,
			NotADB = 26,
			Row = 100,
			Done = 101
			}
			
		[Flags]
		public enum OpenOption : int
			{
			ReadOnly = 0x00000001,
			ReadWrite = 0x00000002,
			Create = 0x00000004,
			NoMutex = 0x00008000,
			FullMutex = 0x00010000
			}
			
		public enum DestructorOption : int
			{
			Static = 0,
			Transient = -1
			}
			


		// Group: Native Functions
		// __________________________________________________________________________


		[DllImport ("NaturalDocs.Engine.SQLite.dll")]
		extern static private Result sqlite3_initialize ();
		
		[DllImport ("NaturalDocs.Engine.SQLite.dll")]
		extern static private Result sqlite3_shutdown ();
		
		[DllImport ("NaturalDocs.Engine.SQLite.dll")]
		extern static private Result sqlite3_open_v2 ([MarshalAs(UnmanagedType.LPStr)] string filename, out IntPtr connectionHandle, 
																	 OpenOption options, IntPtr vfs);

		[DllImport ("NaturalDocs.Engine.SQLite.dll")]
		extern static private Result sqlite3_close (IntPtr connectionHandle);

		[DllImport ("NaturalDocs.Engine.SQLite.dll")]
		extern static private Result sqlite3_extended_result_codes(IntPtr connectionHandle, int onoff);

		#if SQLITE_UTF16
		[DllImport ("NaturalDocs.Engine.SQLite.dll")]
		extern static private Result sqlite3_prepare16_v2 (IntPtr connectionHandle, 
																			[MarshalAs(UnmanagedType.LPWStr)] string statementText, int statementTextByteLength,
																			out IntPtr statementHandle, out IntPtr unusedStatementText);
		#elif SQLITE_UTF8																					
		[DllImport ("NaturalDocs.Engine.SQLite.dll")]
		extern static private Result sqlite3_prepare_v2 (IntPtr connectionHandle, 
																		[MarshalAs(UnmanagedType.LPStr)] string statementText, int statementTextByteLength,
																		out IntPtr statementHandle, out IntPtr unusedStatementText);
		#endif
																					
		[DllImport ("NaturalDocs.Engine.SQLite.dll")]
		extern static private Result sqlite3_bind_int (IntPtr statementHandle, int index, int value);
		
		[DllImport ("NaturalDocs.Engine.SQLite.dll")]
		extern static private Result sqlite3_bind_int64 (IntPtr statementHandle, int index, long value);
		
		#if SQLITE_UTF16
		[DllImport ("NaturalDocs.Engine.SQLite.dll")]
		extern static private Result sqlite3_bind_text16 (IntPtr statementHandle, int index, [MarshalAs(UnmanagedType.LPWStr)] string value, 
																		 int valueByteLength, DestructorOption destructor);
		#elif SQLITE_UTF8
		[DllImport ("NaturalDocs.Engine.SQLite.dll")]
		extern static private Result sqlite3_bind_text (IntPtr statementHandle, int index, [MarshalAs(UnmanagedType.LPStr)] string value, 
																 	  int valueByteLength, DestructorOption destructor);
		#endif
																		  
		[DllImport ("NaturalDocs.Engine.SQLite.dll")]
		extern static private Result sqlite3_bind_double (IntPtr statementHandle, int index, double value);
		
		[DllImport ("NaturalDocs.Engine.SQLite.dll")]
		extern static private Result sqlite3_bind_null (IntPtr  statementHandle, int index);
																					
		[DllImport ("NaturalDocs.Engine.SQLite.dll")]
		extern static private Result sqlite3_step (IntPtr statementHandle);
		
		[DllImport ("NaturalDocs.Engine.SQLite.dll")]
		extern static private int sqlite3_column_int (IntPtr statementHandle, int column);
		
		[DllImport ("NaturalDocs.Engine.SQLite.dll")]
		extern static private long sqlite3_column_int64 (IntPtr statementHandle, int column);
		
		#if SQLITE_UTF16
		[DllImport ("NaturalDocs.Engine.SQLite.dll")]
		extern static private IntPtr sqlite3_column_text16 (IntPtr statementHandle, int column);
		#elif SQLITE_UTF8
		[DllImport ("NaturalDocs.Engine.SQLite.dll")]
		extern static private IntPtr sqlite3_column_text (IntPtr statementHandle, int column);
		#endif
		
		[DllImport ("NaturalDocs.Engine.SQLite.dll")]
		extern static private double sqlite3_column_double (IntPtr statementHandle, int column);
		
		[DllImport ("NaturalDocs.Engine.SQLite.dll")]
		extern static private Result sqlite3_reset (IntPtr statementHandle);
		
		[DllImport ("NaturalDocs.Engine.SQLite.dll")]
		extern static private Result sqlite3_clear_bindings (IntPtr statementHandle);
																					  
		[DllImport ("NaturalDocs.Engine.SQLite.dll")]
		extern static private Result sqlite3_finalize (IntPtr statementHandle);
		
		[DllImport ("NaturalDocs.Engine.SQLite.dll")]
		extern static private IntPtr sqlite3_libversion ();
		


		// Group: Public Aliases
		// __________________________________________________________________________


		static public Result Initialize ()
			{  return sqlite3_initialize();  }
			
		static public Result ShutDown ()
			{  return sqlite3_shutdown();  }
			
		static public Result OpenV2 (string filename, out IntPtr connectionHandle, OpenOption options)
			{  return sqlite3_open_v2(filename, out connectionHandle, options, IntPtr.Zero);  }

		static public Result Close (IntPtr connectionHandle)
			{  return sqlite3_close(connectionHandle);  }

		static public Result ExtendedResultCodes (IntPtr connectionHandle, bool onoff)
			{  return sqlite3_extended_result_codes (connectionHandle, (onoff ? 1 : 0));  }

		static public Result PrepareV2 (IntPtr connectionHandle, string statementText, out IntPtr statementHandle)
			{  
			IntPtr ignore;

			#if SQLITE_UTF16
				// * 2 because it wants the length in bytes, not in characters, and we're passing it as UTF-16.
				return sqlite3_prepare16_v2(connectionHandle, statementText, statementText.Length * 2, out statementHandle, out ignore);
			#elif SQLITE_UTF8
				return sqlite3_prepare_v2(connectionHandle, statementText, statementText.Length, out statementHandle, out ignore);
			#else
				throw new Exception("Did not define SQLITE_UTF8 or SQLITE_UTF16");
			#endif
			}
														   
		static public Result BindInt (IntPtr statementHandle, int index, int value)
			{  return sqlite3_bind_int(statementHandle, index, value);  }
		
		static public Result BindInt64 (IntPtr statementHandle, int index, long value)
			{  return sqlite3_bind_int64(statementHandle, index, value);  }
		
		static public Result BindText (IntPtr statementHandle, int index, string value)
			{  
			#if SQLITE_UTF16
				// * 2 because it wants the length in bytes, not in characters, and we're passing it as UTF-16.
				return sqlite3_bind_text16(statementHandle, index, value, value.Length * 2, DestructorOption.Transient);  
			#elif SQLITE_UTF8
				return sqlite3_bind_text(statementHandle, index, value, value.Length, DestructorOption.Transient);  
			#else
				throw new Exception("Did not define SQLITE_UTF8 or SQLITE_UTF16");
			#endif
			}
			
		static public Result BindDouble (IntPtr statementHandle, int index, double value)
			{  return sqlite3_bind_double (statementHandle, index, value);  }
			
		static public Result BindNull (IntPtr statementHandle, int index)
			{  return sqlite3_bind_null (statementHandle, index);  }

		static public Result Step (IntPtr statementHandle)
			{  return sqlite3_step(statementHandle);  }

		static public int ColumnInt (IntPtr statementHandle, int column)
			{  return sqlite3_column_int (statementHandle, column);  }
			
		static public long ColumnInt64 (IntPtr statementHandle, int column)
			{  return sqlite3_column_int64 (statementHandle, column);  }
			
		static public string ColumnText (IntPtr statementHandle, int column)
			{
			// We can't use the string type as a return value for the API call or else C# will try to deallocate it, which
			// it shouldn't.  It may not crash on .NET but it definitely does in Mono.
			
			#if SQLITE_UTF16
				IntPtr nativeResult = sqlite3_column_text16 (statementHandle, column);
				return Marshal.PtrToStringUni(nativeResult);
			#elif SQLITE_UTF8
				IntPtr nativeResult = sqlite3_column_text (statementHandle, column);
				return Marshal.PtrToStringAnsi(nativeResult);
			#else
				throw new Exception("Did not define SQLITE_UTF8 or SQLITE_UTF16");
			#endif
			}
			
		static public double ColumnDouble (IntPtr statementHandle, int column)
			{  return sqlite3_column_double(statementHandle, column);  }
			
		static public Result Reset (IntPtr statementHandle)
			{  return sqlite3_reset(statementHandle);  }
			
		static public Result ClearBindings (IntPtr statementHandle)
			{  return sqlite3_clear_bindings(statementHandle);  }

		static public Result Finalize (IntPtr statementHandle)
			{  return sqlite3_finalize(statementHandle);  }

		static public string LibVersion ()
			{
			IntPtr nativeResult = sqlite3_libversion();
			return Marshal.PtrToStringAnsi(nativeResult);
			}

		}
	}