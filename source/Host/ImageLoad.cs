using UnityEngine;

namespace Trellis
{
	internal delegate bool ImageLoad(string name, TextureWrapMode wrapMode, out Texture2D texture, out string status, out ImagePreview preview);
}
