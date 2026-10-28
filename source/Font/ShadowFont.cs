using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace Trellis
{
	// imgui binds one sdf material per font instance so a clone carries a softened shadow material of its own
	internal static class ShadowFont
	{
		private const string kImguiShader = "Hidden/TextCore/Distance Field SSD";
		private const string kSoftnessProperty = "_OutlineSoftness";
		private const string kDilateProperty = "_FaceDilate";
		private const string kCloneSuffix = " Shadow";
		private const string kCachedAssetMethod = nameof(TextSettings.GetCachedFontAsset);

		private static readonly Dictionary<IntPtr, Font> kClones = new Dictionary<IntPtr, Font>();

		// unity 6 dropped the shader parameter and a direct call to the missing overload aborts the jit
		private static readonly MethodInfo kCachedAsset = typeof(TextSettings).GetMethod(ShadowFont.kCachedAssetMethod, new[] { typeof(Font), typeof(Shader) }) ?? typeof(TextSettings).GetMethod(ShadowFont.kCachedAssetMethod, new[] { typeof(Font) });

		private static class Shape
		{
			// past about 0.8 combined the blur overruns the glyph quad padding and draws boxes
			internal const float kSoftness = 0.35f;
			internal const float kDilate = 0.2f;
		}

		// the clone is never destroyed because imgui keeps its font asset cached and a stale text handle would draw a dead material
		internal static Font of(Font source)
		{
			if (!Live.isUnityAlive(source))
				return source;

			if (ShadowFont.kClones.TryGetValue(source.Pointer, out Font cached) && Live.isUnityAlive(cached))
				return cached;

			Font clone = ShadowFont.create(source);
			ShadowFont.kClones[source.Pointer] = clone;
			return clone;
		}

		private static Font create(Font source)
		{
			try
			{
				Shader shader = Shader.Find(ShadowFont.kImguiShader);
				if (!Live.isUnityAlive(shader))
				{
					UiLog.warn($"[ShadowFont.create] {ShadowFont.kImguiShader} missing, shadows stay hard");
					return source;
				}

				Font clone = UnityEngine.Object.Instantiate(source);
				clone.name = source.name + ShadowFont.kCloneSuffix;
				clone.hideFlags = HideFlags.DontUnloadUnusedAsset;

				FontAsset asset = ShadowFont.cachedAsset(clone, shader);
				Material material = Live.isUnityAlive(asset) ? asset.material : null;
				if (!Live.isUnityAlive(material))
				{
					UiLog.warn($"[ShadowFont.create] no font asset material for {source.name}, shadows stay hard");
					UnityEngine.Object.Destroy(clone);
					return source;
				}

				material.SetFloat(ShadowFont.kSoftnessProperty, Shape.kSoftness);
				material.SetFloat(ShadowFont.kDilateProperty, Shape.kDilate);

				UiLog.debug($"[ShadowFont.create] softened shadow material for {source.name}");
				return clone;
			}
			catch (Exception ex)
			{
				UiLog.logException("ShadowFont.create", ex);
				return source;
			}
		}

		// same cache key imgui uses so this is the asset its text handles draw with
		private static FontAsset cachedAsset(Font font, Shader shader)
		{
			if (ShadowFont.kCachedAsset == null)
				return null;

			object[] args = ShadowFont.kCachedAsset.GetParameters().Length == 2 ? new object[] { font, shader } : new object[] { font };
			return ShadowFont.kCachedAsset.Invoke(RuntimeTextSettings.defaultTextSettings, args) as FontAsset;
		}
	}
}
