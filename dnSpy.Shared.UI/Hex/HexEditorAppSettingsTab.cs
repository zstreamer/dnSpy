﻿/*
    Copyright (C) 2014-2015 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Shared.UI.Controls;
using dnSpy.Shared.UI.HexEditor;
using dnSpy.Shared.UI.MVVM;

namespace dnSpy.Shared.UI.Hex {
	[Export(typeof(IAppSettingsTabCreator))]
	sealed class HexEditorAppSettingsTabCreator : IAppSettingsTabCreator {
		readonly HexEditorSettingsImpl hexEditorSettingsImpl;

		[ImportingConstructor]
		HexEditorAppSettingsTabCreator(HexEditorSettingsImpl hexEditorSettingsImpl) {
			this.hexEditorSettingsImpl = hexEditorSettingsImpl;
		}

		public IEnumerable<IAppSettingsTab> Create() {
			yield return new HexEditorAppSettingsTab(hexEditorSettingsImpl);
		}
	}

	sealed class HexEditorAppSettingsTab : IAppSettingsTab {
		public double Order {
			get { return AppSettingsConstants.ORDER_SETTINGS_TAB_HEXEDITOR; }
		}

		public string Title {
			get { return "Hex Editor"; }
		}

		public object UIObject {
			get { return displayAppSettingsVM; }
		}

		readonly HexEditorSettingsImpl hexEditorSettingsImpl;
		readonly HexEditorAppSettingsVM displayAppSettingsVM;

		public HexEditorAppSettingsTab(HexEditorSettingsImpl hexEditorSettingsImpl) {
			this.hexEditorSettingsImpl = hexEditorSettingsImpl;
			this.displayAppSettingsVM = new HexEditorAppSettingsVM(hexEditorSettingsImpl.Clone());
		}

		public void OnClosed(bool saveSettings, IAppRefreshSettings appRefreshSettings) {
			if (!saveSettings)
				return;

			displayAppSettingsVM.Settings.CopyTo(hexEditorSettingsImpl);
		}
	}

	sealed class HexEditorAppSettingsVM : ViewModelBase {
		public HexEditorSettings Settings {
			get { return hexEditorSettings; }
		}
		readonly HexEditorSettings hexEditorSettings;

		public Int32VM BytesGroupCountVM {
			get { return bytesGroupCountVM; }
		}
		readonly Int32VM bytesGroupCountVM;

		public Int32VM BytesPerLineVM {
			get { return bytesPerLineVM; }
		}
		readonly Int32VM bytesPerLineVM;

		public FontFamily[] FontFamilies {
			get { return fontFamilies; }
			set {
				if (fontFamilies != value) {
					fontFamilies = value;
					OnPropertyChanged("FontFamilies");
				}
			}
		}
		FontFamily[] fontFamilies;

		public FontFamily FontFamily {
			get { return fontFamily; }
			set {
				if (fontFamily != value) {
					fontFamily = value;
					hexEditorSettings.FontFamily = fontFamily;
					OnPropertyChanged("FontFamilyVM");
				}
			}
		}
		FontFamily fontFamily;

		public EnumListVM AsciiEncodingVM {
			get { return asciiEncodingVM; }
		}
		readonly EnumListVM asciiEncodingVM;
		readonly EnumVM[] asciiEncodingList = new EnumVM[] {
			new EnumVM(AsciiEncoding.ASCII, "ASCII"),
			new EnumVM(AsciiEncoding.ANSI, "ANSI"),
			new EnumVM(AsciiEncoding.UTF7, "UTF-7"),
			new EnumVM(AsciiEncoding.UTF8, "UTF-8"),
			new EnumVM(AsciiEncoding.UTF32, "UTF-32"),
			new EnumVM(AsciiEncoding.Unicode, "Unicode"),
			new EnumVM(AsciiEncoding.BigEndianUnicode, "BE Unicode"),
		};

		public HexEditorAppSettingsVM(HexEditorSettings hexEditorSettings) {
			this.hexEditorSettings = hexEditorSettings;
			this.asciiEncodingVM = new EnumListVM(asciiEncodingList, (a, b) => hexEditorSettings.AsciiEncoding = (AsciiEncoding)AsciiEncodingVM.SelectedItem);
			this.bytesGroupCountVM = new Int32VM(a => { HasErrorUpdated(); hexEditorSettings.BytesGroupCount = BytesGroupCountVM.Value; });
			this.bytesPerLineVM = new Int32VM(a => { HasErrorUpdated(); hexEditorSettings.BytesPerLine = BytesPerLineVM.Value; }) {
				Min = 0,
				Max = HexEditorSettings.MAX_BYTES_PER_LINE,
			};
			AsciiEncodingVM.SelectedItem = hexEditorSettings.AsciiEncoding;
			BytesGroupCountVM.Value = hexEditorSettings.BytesGroupCount;
			BytesPerLineVM.Value = hexEditorSettings.BytesPerLine;
			FontFamily = hexEditorSettings.FontFamily;
			Task.Factory.StartNew(() => FontUtils.GetMonospacedFonts())
			.ContinueWith(t => {
				var ex = t.Exception;
				FontFamilies = t.Result;
			}, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
		}
	}
}