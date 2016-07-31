using System.IO;
using UnityEngine;

namespace UltimateCam
{
	public class UltimateCross : MonoBehaviour
	{
		private readonly Texture2D texture;

		public UltimateCross()
		{
			Stream cs = UltimateMain.Instance.getAssembly().GetManifestResourceStream("UltimateCam.UltimateCross.png");
			if (cs == null)
			{
				UltimateMain.Instance.Log("Can't load crosshair from assembly!", UltimateMain.LogLevel.ERROR);
				texture = Texture2D.blackTexture;
				return;
			}
			texture = new Texture2D(1, 1);
			int l = (int)cs.Length;
			byte[] buffer = new byte[l];
			int r = 0;
			while(r < l)
				r += cs.Read(buffer, r, l - r);
			texture.LoadImage(buffer);
			cs.Close();
		}

		public void OnGUI()
		{
			GUI.depth = -999;
			GUI.DrawTexture(new Rect((Screen.width / 2.0f) - (texture.width / 2.0f), (Screen.height / 2.0f) - (texture.height / 2.0f), texture.width, texture.height), texture);
		}
	}
}

