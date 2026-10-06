using System;
using ModStatistik;

class DoublePressTests
{
    static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    static void Main()
    {
        var input = new DoublePress();
        Require(!input.Update(1f, true), "Einzel-I darf keinen Mod-Tab ausloesen.");
        Require(input.Update(1.3f, true), "Doppel-I muss ausloesen.");
        Require(!input.Update(1.4f, true), "Dritter Druck darf nicht erneut ausloesen.");
        input.Reset();
        Require(!input.Update(2f, true), "Neuer erster Druck.");
        Require(!input.Update(2.5f, true), "Zu spaeter Druck darf kein Doppel-I sein.");
        input.Reset();
        Require(!input.Update(3f, true), "Erster Druck vor Textfeld.");
        input.Reset();
        Require(!input.Update(3.2f, true), "Reset bei Texteingabe muss Doppel-I verhindern.");
        input.Reset();
        Require(!input.Update(5f, true) && !input.Update(1f, true), "Zeitwechsel darf nicht ausloesen.");
        Console.WriteLine("Doppel-I: Einzelpressen, Doppelpressen, Ablauf und Eingabe-Reset geprueft.");
    }
}
