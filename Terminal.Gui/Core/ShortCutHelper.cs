﻿using NStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terminal.Gui {
	/// <summary>
	/// Represents a helper to manipulate shortcut keys used on views.
	/// </summary>
	public class ShortCutHelper {
		private Key shortCut;

		/// <summary>
		/// This is the global setting that can be used as a global shortcut to invoke the action on the view.
		/// </summary>
		public virtual Key ShortCut {
			get => shortCut;
			set {
				if (shortCut != value && (PostShortCutValidation (value) || value == Key.Null)) {
					shortCut = value;
				}
			}
		}

		/// <summary>
		/// The keystroke combination used in the <see cref="ShortCut"/> as string.
		/// </summary>
		public virtual ustring ShortCutTag => GetShortCutTag (shortCut);

		/// <summary>
		/// Get the <see cref="ShortCut"/> key as string.
		/// </summary>
		/// <param name="shortCut">The shortcut key.</param>
		/// <returns></returns>
		public static ustring GetShortCutTag (Key shortCut)
		{
			if (shortCut == Key.Null) {
				return "";
			}

			var k = shortCut;
			var delimiter = MenuBar.ShortCutDelimiter;
			ustring tag = ustring.Empty;
			var sCut = GetKeyToString (k, out Key knm).ToString ();
			if (knm == Key.Unknown) {
				k &= ~Key.Unknown;
				sCut = GetKeyToString (k, out _).ToString ();
			}
			if ((k & Key.CtrlMask) != 0) {
				tag = "Ctrl";
			}
			if ((k & Key.ShiftMask) != 0) {
				if (!tag.IsEmpty) {
					tag += delimiter;
				}
				tag += "Shift";
			}
			if ((k & Key.AltMask) != 0) {
				if (!tag.IsEmpty) {
					tag += delimiter;
				}
				tag += "Alt";
			}

			ustring [] keys = ustring.Make (sCut).Split (",");
			for (int i = 0; i < keys.Length; i++) {
				var key = keys [i].TrimSpace ();
				if (key == Key.AltMask.ToString () || key == Key.ShiftMask.ToString () || key == Key.CtrlMask.ToString ()) {
					continue;
				}
				if (!tag.IsEmpty) {
					tag += delimiter;
				}
				if (!key.Contains ("F") && key.Length > 2 && keys.Length == 1) {
					k = (uint)Key.AltMask + k;
					tag += ((char)k).ToString ();
				} else if (key.Length == 2 && key.StartsWith ("D")) {
					tag += ((char)key.ElementAt (1)).ToString ();
				} else {
					tag += key;
				}
			}

			return tag;
		}

		/// <summary>
		/// Return key as string.
		/// </summary>
		/// <param name="key">The key to extract.</param>
		/// <param name="knm">Correspond to the non modifier key.</param>
		public static ustring GetKeyToString (Key key, out Key knm)
		{
			if (key == Key.Null) {
				knm = Key.Null;
				return "";
			}

			knm = key;
			var mK = key & (Key.AltMask | Key.CtrlMask | Key.ShiftMask);
			knm &= ~mK;
			for (uint i = (uint)Key.F1; i < (uint)Key.F12; i++) {
				if (knm == (Key)i) {
					mK |= (Key)i;
				}
			}
			knm &= ~mK;
			uint.TryParse (knm.ToString (), out uint c);
			var s = mK == Key.Null ? "" : mK.ToString ();
			if (s != "" && (knm != Key.Null || c > 0)) {
				s += ",";
			}
			s += c == 0 ? knm == Key.Null ? "" : knm.ToString () : ((char)c).ToString ();
			return s;
		}

		/// <summary>
		/// Allows to retrieve a <see cref="Key"/> from a <see cref="ShortCutTag"/>
		/// </summary>
		/// <param name="tag">The key as string.</param>
		public static Key GetShortCutFromTag (ustring tag)
		{
			var sCut = tag;
			if (sCut.IsEmpty) {
				return default;
			}

			Key key = Key.Null;
			//var hasCtrl = false;
			var delimiter = MenuBar.ShortCutDelimiter;

			ustring [] keys = sCut.Split (delimiter);
			for (int i = 0; i < keys.Length; i++) {
				var k = keys [i];
				if (k == "Ctrl") {
					//hasCtrl = true;
					key |= Key.CtrlMask;
				} else if (k == "Shift") {
					key |= Key.ShiftMask;
				} else if (k == "Alt") {
					key |= Key.AltMask;
				} else if (k.StartsWith ("F") && k.Length > 1) {
					int.TryParse (k.Substring (1).ToString (), out int n);
					for (uint j = (uint)Key.F1; j <= (uint)Key.F12; j++) {
						int.TryParse (((Key)j).ToString ().Substring (1), out int f);
						if (f == n) {
							key |= (Key)j;
						}
					}
				} else {
					key |= (Key)Enum.Parse (typeof (Key), k.ToString ());
				}
			}

			return key;
		}

		/// <summary>
		/// Lookup for a <see cref="Key"/> on range of keys.
		/// </summary>
		/// <param name="key">The source key.</param>
		/// <param name="first">First key in range.</param>
		/// <param name="last">Last key in range.</param>
		public static bool CheckKeysFlagRange (Key key, Key first, Key last)
		{
			for (uint i = (uint)first; i < (uint)last; i++) {
				if ((key | (Key)i) == key) {
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Used at key down or key press validation.
		/// </summary>
		/// <param name="key">The key to validate.</param>
		/// <returns><c>true</c> if is valid.<c>false</c>otherwise.</returns>
		public static bool PreShortCutValidation (Key key)
		{
			if ((key & (Key.CtrlMask | Key.ShiftMask | Key.AltMask)) == 0 && !CheckKeysFlagRange (key, Key.F1, Key.F12)) {
				return false;
			}

			return true;
		}

		/// <summary>
		/// Used at key up validation.
		/// </summary>
		/// <param name="key">The key to validate.</param>
		/// <returns><c>true</c> if is valid.<c>false</c>otherwise.</returns>
		public static bool PostShortCutValidation (Key key)
		{
			GetKeyToString (key, out Key knm);

			if (CheckKeysFlagRange (key, Key.F1, Key.F12) ||
				((key & (Key.CtrlMask | Key.ShiftMask | Key.AltMask)) != 0 && knm != Key.Null && knm != Key.Unknown)) {
				return true;
			}
			return false;
		}
	}
}
