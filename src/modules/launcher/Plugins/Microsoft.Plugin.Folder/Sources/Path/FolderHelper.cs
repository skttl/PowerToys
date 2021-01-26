﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Microsoft.Plugin.Folder.Sources
{
    public class FolderHelper : IFolderHelper
    {
        private readonly IDriveInformation _driveInformation;
        private readonly IFolderLinks _folderLinks;

        public FolderHelper(IDriveInformation driveInformation, IFolderLinks folderLinks)
        {
            _driveInformation = driveInformation;
            _folderLinks = folderLinks;
        }

        public IEnumerable<FolderLink> GetUserFolderResults(string query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(paramName: nameof(query));
            }

            // Using OrdinalIgnoreCase since this is internal
            return _folderLinks.FolderLinks()
                .Where(x => x.Nickname.StartsWith(query, StringComparison.OrdinalIgnoreCase));
        }

        public bool IsDriveOrSharedFolder(string search)
        {
            if (search == null)
            {
                throw new ArgumentNullException(nameof(search));
            }

            if (IsSharedFolder(search))
            {
                return true;
            }

            var driverNames = _driveInformation.GetDriveNames()
                .ToImmutableArray();

            if (driverNames.Any())
            {
                // Using InvariantCultureIgnoreCase since this is searching for drive names
                if (driverNames.Any(dn => search.StartsWith(dn, StringComparison.InvariantCultureIgnoreCase)))
                {
                    // normal drive letter
                    return true;
                }
            }
            else
            {
                if (search.Length > 2 && ValidDriveLetter(search[0]) && search[1] == ':')
                { // when we don't have the drive letters we can try...
                    return true; // we don't know so let's give it the possibility
                }
            }

            return false;
        }

        public bool IsNetworkShare(string search)
        {
            if (search == null)
            {
                throw new ArgumentNullException(nameof(search));
            }

            return IsSharedFolder(search) && search.Trim('\\').Split('\\').Length == 1;
        }

        /// <summary>
        /// This check is needed because char.IsLetter accepts more than [A-z]
        /// </summary>
        public static bool ValidDriveLetter(char c)
        {
            return c <= 122 && char.IsLetter(c);
        }

        public static string Expand(string search)
        {
            if (search == null)
            {
                throw new ArgumentNullException(nameof(search));
            }

            // Absolute path of system drive: \Windows\System32
            if (search[0] == '\\' && (search.Length == 1 || search[1] != '\\'))
            {
                search = Path.Combine(Path.GetPathRoot(Environment.SystemDirectory), search.Substring(1));
            }

            return Environment.ExpandEnvironmentVariables(search);
        }

        private static bool IsSharedFolder(string search)
        {
            // Using Ordinal this is internal and we're comparing symbols
            return search.StartsWith(@"\\", StringComparison.Ordinal);
        }
    }
}
