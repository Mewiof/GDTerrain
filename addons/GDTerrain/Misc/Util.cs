using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;

namespace GDTerrain {

	public static class Util {

		/// <returns>
		/// 16 -> 16
		/// <para>17 -> 32</para>
		/// </returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int NextPowerOfTwo(int value) {
			value -= 1;
			value |= value >> 1;
			value |= value >> 2;
			value |= value >> 4;
			value |= value >> 8;
			value |= value >> 16;
			value += 1;
			return value;
		}

		/// <summary>Performs positive int division with rounding to greater</summary>
		/// <returns>
		/// 4 / 2 -> 2
		/// <para>5 / 3 -> 2</para>
		/// </returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int UpDiv(int a, int b) {
			if (a % b != 0) {
				return a / b + 1;
			}
			return a / b;
		}

		#region GetSegmentClippedByRect
		private static readonly List<Vector2> _getSegClByRectHitsCache = new();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static List<Vector2> GetSegmentClippedByRect(Rect2 rect, Vector2 segmentBegin, Vector2 segmentEnd) {
			/*        /
			 * A-----/---B      A-----+---B
			 * |    /    |  =>  |    /    |
			 * |   /     |      |   /     |
			 * C--/------D      C--+------D
			 *   /
			 */

			_getSegClByRectHitsCache.Clear();

			if (rect.HasPoint(segmentBegin) && rect.HasPoint(segmentEnd)) {
				_getSegClByRectHitsCache.Add(segmentBegin);
				_getSegClByRectHitsCache.Add(segmentEnd);
				return _getSegClByRectHitsCache;
			}

			Vector2 a = rect.Position;
			Vector2 b = new(rect.End.x, rect.Position.y);
			Vector2 c = new(rect.Position.x, rect.End.y);
			Vector2 d = rect.End;

			Variant aB = Geometry2D.SegmentIntersectsSegment(segmentBegin, segmentEnd, a, b);
			Variant cD = Geometry2D.SegmentIntersectsSegment(segmentBegin, segmentEnd, c, d);
			Variant aC = Geometry2D.SegmentIntersectsSegment(segmentBegin, segmentEnd, a, c);
			Variant bD = Geometry2D.SegmentIntersectsSegment(segmentBegin, segmentEnd, b, d);

			if (aB.VariantType != Variant.Type.Nil) {
				_getSegClByRectHitsCache.Add(aB.AsVector2());
			}
			if (cD.VariantType != Variant.Type.Nil) {
				_getSegClByRectHitsCache.Add(cD.AsVector2());
			}
			if (aC.VariantType != Variant.Type.Nil) {
				_getSegClByRectHitsCache.Add(aC.AsVector2());
			}
			if (bD.VariantType != Variant.Type.Nil) {
				_getSegClByRectHitsCache.Add(bD.AsVector2());
			}

			// now we need to order hits
			if (_getSegClByRectHitsCache.Count == 1) {
				if (rect.HasPoint(segmentBegin)) {
					Vector2 hit = _getSegClByRectHitsCache[0];
					_getSegClByRectHitsCache.Clear();
					_getSegClByRectHitsCache.Add(segmentBegin);
					_getSegClByRectHitsCache.Add(hit);
				} else if (rect.HasPoint(segmentEnd)) {
					Vector2 hit = _getSegClByRectHitsCache[0];
					_getSegClByRectHitsCache.Clear();
					_getSegClByRectHitsCache.Add(hit);
					_getSegClByRectHitsCache.Add(segmentEnd);
				} else {
					_getSegClByRectHitsCache.Clear();
					return _getSegClByRectHitsCache;
				}
			} else if (_getSegClByRectHitsCache.Count == 2) {
				Vector2 h0 = _getSegClByRectHitsCache[0];
				Vector2 h1 = _getSegClByRectHitsCache[1];
				float d0 = h0.DistanceSquaredTo(segmentBegin);
				float d1 = h1.DistanceSquaredTo(segmentBegin);
				if (d0 > d1) {
					_getSegClByRectHitsCache.Clear();
					_getSegClByRectHitsCache.Add(h1);
					_getSegClByRectHitsCache.Add(h0);
				}
			}

			return _getSegClByRectHitsCache;
		}
		#endregion

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 XZ(this Vector3 value) {
			return new(value.x, value.z);
		}

		#region GridRaytrace2D
		public struct GridRaytraceResult2D {

			public Vector2 hitCellPos, prevCellPos;

			public GridRaytraceResult2D(Vector2 hitCellPos, Vector2 prevCellPos) {
				this.hitCellPos = hitCellPos;
				this.prevCellPos = prevCellPos;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static GridRaytraceResult2D? GridRaytrace2D(Vector2 rayOrigin, Vector2 rayDirection, Func<int, int, float, float, bool> quadPredicate, float maxDistance) {
			if (maxDistance < .0001f) {
				// we can consider ray too short to hit anything
				return null;
			}

			int xIStep = 0;
			if (rayDirection.x > 0f) {
				xIStep = 1;
			} else if (rayDirection.x < 0f) {
				xIStep = -1;
			}

			int yIStep = 0;
			if (rayDirection.y > 0f) {
				yIStep = 1;
			} else if (rayDirection.y < 0f) {
				yIStep = -1;
			}

			float paramDeltaX = float.PositiveInfinity;
			if (xIStep != 0) {
				paramDeltaX = 1f / MathF.Abs(rayDirection.x);
			}

			float paramDeltaY = float.PositiveInfinity;
			if (yIStep != 0) {
				paramDeltaY = 1f / MathF.Abs(rayDirection.y);
			}

			float paramCrossX;
			float paramCrossY;

			if (xIStep != 0) {
				if (xIStep == 1) {
					paramCrossX = (Mathf.Ceil(rayOrigin.x) - rayOrigin.x) * paramDeltaX;
				} else {
					paramCrossX = (rayOrigin.x - MathF.Floor(rayOrigin.x)) * paramDeltaX;
				}
			} else {
				paramCrossX = float.PositiveInfinity;
			}

			if (yIStep != 0) {
				if (yIStep == 1) {
					paramCrossY = (Mathf.Ceil(rayOrigin.y) - rayOrigin.y) * paramDeltaY;
				} else {
					paramCrossY = (rayOrigin.y - MathF.Floor(rayOrigin.y)) * paramDeltaY;
				}
			} else {
				paramCrossY = float.PositiveInfinity;
			}

			int x = (int)MathF.Floor(rayOrigin.x);
			int y = (int)MathF.Floor(rayOrigin.y);

			// workaround for cases where the ray starts from an integer position
			if (paramCrossX == 0f) {
				paramCrossX += paramDeltaX;
				// when moving backward, we must ignore the position we would get using the above flooring,
				// because the ray is not moving in that direction
				if (xIStep == -1) {
					x--;
				}
			}

			if (paramDeltaY == 0f) {
				paramCrossY += paramDeltaY;
				if (yIStep == -1) {
					y--;
				}
			}

			int prevX;
			int prevY;
			float param = 0f;
			float prevParam;

			while (true) {
				prevX = x;
				prevY = y;
				prevParam = param;

				if (paramCrossX < paramCrossY) {
					// x lane
					x += xIStep;
					// assign before advancing the parameter to be synchronized with the initialization step
					param = paramCrossX;
					paramCrossX += paramDeltaX;
				} else {
					// y lane
					y += yIStep;
					param = paramCrossY;
					paramCrossY += paramDeltaY;
				}

				if (param > maxDistance) {
					param = maxDistance;
					// quad coordinates, enter param, exit/end param
					if (quadPredicate.Invoke(prevX, prevY, prevParam, param)) {
						GridRaytraceResult2D res = new(new(x, y), new(prevX, prevY));
						return res;
					} else {
						break;
					}
				} else if (quadPredicate.Invoke(prevX, prevY, prevParam, param)) {
					return new(new(x, y), new(prevX, prevY));
				}
			}

			return null;
		}
		#endregion

		/// <summary>
		/// Clamps <paramref name="x"/>, <paramref name="y"/> to width/hegiht of <paramref name="im"/>
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Color GetPixelClamped(Image im, int x, int y) {
			int imWidth = im.GetWidth();
			int imHeight = im.GetHeight();

			if (x < 0) {
				x = 0;
			} else if (x >= imWidth) {
				x = imWidth - 1;
			}

			if (y < 0) {
				y = 0;
			} else if (y >= imHeight) {
				y = imHeight - 1;
			}

			return im.GetPixel(x, y);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static (Rect2i, Vector2i) GetCroppedImageParams(int sourceW, int sourceH, int destW, int destH, Vector2i anchor) {
			Vector2i relAnchor = (anchor + Vector2i.One) / 2;

			int destX = (destW - sourceW) * relAnchor.x;
			int destY = (destH - sourceH) * relAnchor.y;

			int sourceX = 0;
			int sourceY = 0;

			if (destX < 0) {
				sourceX -= destX;
				sourceW -= destX;
				destX = 0;
			}

			if (destY < 0) {
				sourceY -= destY;
				sourceH -= destY;
				destY = 0;
			}

			if (destX + sourceW >= destW) {
				sourceW = destW - destX;
			}

			if (destY + sourceH >= destH) {
				sourceH = destH - destY;
			}

			return (new(sourceX, sourceY, sourceW, sourceH), new(destX, destY));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Image GetCroppedImage(Image source, int width, int height, Color? fill, Vector2i anchor) {
			int sourceWidth = source.GetWidth();
			int sourceHeight = source.GetHeight();
			if (width == sourceWidth && height == sourceHeight) {
				return source;
			}
			Image result = Image.Create(width, height, false, source.GetFormat());
			if (fill.HasValue) {
				result.Fill(fill.Value);
			}
			(Rect2i sourceRect, Vector2i destPos) = GetCroppedImageParams(sourceWidth, sourceHeight, width, height, anchor);
			result.BlitRect(source, sourceRect, destPos);
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 GetRedRange(Image image, Rect2i rect) {
			IntRange2D range = new(rect);
			range.Clip(image.GetSize());

			float minVal = image.GetPixel(range.MinX, range.MinY).r;
			float maxVal = minVal;

			for (int y = range.MinY; y < range.MaxY; y++) {
				for (int x = range.MinX; x < range.MaxX; x++) {
					float v = image.GetPixel(x, y).r;

					if (v > maxVal) {
						maxVal = v;
					} else if (v < minVal) {
						minVal = v;
					}
				}
			}

			return new(minVal, maxVal);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Clamp(int value, int min, int max) {
			if (value < min) {
				return min;
			}
			if (value > max) {
				return max;
			}
			return value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T[] Duplicate<T>(this T[] value) {
			T[] result = new T[value.Length];
			for (int i = 0; i < result.Length; i++) {
				result[i] = value[i];
			}
			return result;
		}
	}
}
