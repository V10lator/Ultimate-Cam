using System;
using UnityEngine;

namespace UltimateCam
{
	public class UltimateCross : MonoBehaviour
	{
		private readonly Color c;
		private readonly float width = 15.0f;
		private readonly float space = 5.0f;

		public UltimateCross()
		{
			c = Color.black;
			c.a = 0.25f;
		}

		public void OnGUI()
		{
			float middleX = Screen.width / 2.0f;
			float middleY = Screen.height / 2.0f;
			float mod = width + space;

			GUI.depth = -999;
			GUI.color = c;
			GUI.DrawTexture(new Rect(middleX - mod, middleY - 0.5f, width, 1.0f), Texture2D.whiteTexture);
			GUI.DrawTexture(new Rect(middleX + space, middleY - 0.5f, width, 1.0f), Texture2D.whiteTexture);
			GUI.DrawTexture(new Rect(middleX - 0.5f, middleY - mod, 1.0f, width), Texture2D.whiteTexture);
			GUI.DrawTexture(new Rect(middleX - 0.5f, middleY + space, 1.0f, width), Texture2D.whiteTexture);
		}
	}
}

