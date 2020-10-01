﻿using System;
using Terminal.Gui;

namespace ReactiveExample {
	public static class Program {
		static void Main (string [] args) {
			Application.Init (); // A hacky way to enable instant UI updates.
			Application.MainLoop.AddTimeout(TimeSpan.FromSeconds(0.1), a => true);
			Application.Run (new LoginView (new LoginViewModel ()));
		}
	}
}