﻿#pragma checksum "H:\MyProj\Points\Points\MainPage.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "72F7D6B5DB4DB0DCEA531D1DC29760C0"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Points
{
    partial class MainPage : 
        global::Windows.UI.Xaml.Controls.Page, 
        global::Windows.UI.Xaml.Markup.IComponentConnector,
        global::Windows.UI.Xaml.Markup.IComponentConnector2
    {
        /// <summary>
        /// Connect()
        /// </summary>
        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 14.0.0.0")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void Connect(int connectionId, object target)
        {
            switch(connectionId)
            {
            case 1:
                {
                    global::Windows.UI.Xaml.Controls.Page element1 = (global::Windows.UI.Xaml.Controls.Page)(target);
                    #line 9 "..\..\..\MainPage.xaml"
                    ((global::Windows.UI.Xaml.Controls.Page)element1).Unloaded += this.Page_Unloaded;
                    #line default
                }
                break;
            case 2:
                {
                    this.canvas = (global::Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl)(target);
                    #line 12 "..\..\..\MainPage.xaml"
                    ((global::Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl)this.canvas).Draw += this.canvas_Draw;
                    #line 12 "..\..\..\MainPage.xaml"
                    ((global::Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl)this.canvas).Tapped += this.canvas_Tapped;
                    #line 12 "..\..\..\MainPage.xaml"
                    ((global::Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl)this.canvas).PointerMoved += this.canvas_PointerMoved;
                    #line default
                }
                break;
            case 3:
                {
                    this.NewGame = (global::Windows.UI.Xaml.Controls.AppBarButton)(target);
                    #line 25 "..\..\..\MainPage.xaml"
                    ((global::Windows.UI.Xaml.Controls.AppBarButton)this.NewGame).Tapped += this.NewGame_Tapped;
                    #line default
                }
                break;
            case 4:
                {
                    this.SaveGame = (global::Windows.UI.Xaml.Controls.AppBarButton)(target);
                    #line 26 "..\..\..\MainPage.xaml"
                    ((global::Windows.UI.Xaml.Controls.AppBarButton)this.SaveGame).Tapped += this.SaveGame_Tapped;
                    #line default
                }
                break;
            case 5:
                {
                    this.LoadGame = (global::Windows.UI.Xaml.Controls.AppBarButton)(target);
                    #line 27 "..\..\..\MainPage.xaml"
                    ((global::Windows.UI.Xaml.Controls.AppBarButton)this.LoadGame).Tapped += this.LoadGame_Tapped;
                    #line default
                }
                break;
            case 6:
                {
                    this.Autoplay = (global::Windows.UI.Xaml.Controls.AppBarButton)(target);
                    #line 28 "..\..\..\MainPage.xaml"
                    ((global::Windows.UI.Xaml.Controls.AppBarButton)this.Autoplay).Tapped += this.Settings_Tapped;
                    #line default
                }
                break;
            default:
                break;
            }
            this._contentLoaded = true;
        }

        [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Windows.UI.Xaml.Build.Tasks"," 14.0.0.0")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public global::Windows.UI.Xaml.Markup.IComponentConnector GetBindingConnector(int connectionId, object target)
        {
            global::Windows.UI.Xaml.Markup.IComponentConnector returnValue = null;
            return returnValue;
        }
    }
}

