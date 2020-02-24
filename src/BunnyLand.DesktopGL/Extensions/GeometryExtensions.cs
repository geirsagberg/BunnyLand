using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using Vector3 = System.Numerics.Vector3;

namespace BunnyLand.DesktopGL.Extensions
{
    public static class GeometryExtensions
    {
        public static Vector2 Scale(this Vector2 vector2, float scale) =>
            new Vector2(vector2.X * scale, vector2.Y * scale);

        public static Vector2 NormalizedOrZero(this Vector2 vector2) =>
            vector2 == Vector2.Zero ? Vector2.Zero : Vector2.Normalize(vector2);

        public static Vector2 WidthVector(this Size2 size) => new Vector2(size.Width, 0);
        public static Vector2 HeightVector(this Size2 size) => new Vector2(0, size.Height);

        public static Vector2 WidthVector(this RectangleF rectangle) => new Vector2(rectangle.Width, 0);
        public static Vector2 HeightVector(this RectangleF rectangle) => new Vector2(0, rectangle.Height);

        public static bool IsBetween(this float f, float a, float b) => f >= (a < b ? a : b) && f <= (a < b ? b : a);

        public static bool Contains(this RectangleF first, RectangleF second) =>
            first.Contains(second.TopLeft) && first.Contains(second.BottomRight);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 SubtractLength(this Vector2 vector, float length) =>
            vector.NormalizedOrZero() * (vector.Length() - length);

        public static RectangleF Expand(this RectangleF rectangle, Vector2 direction)
        {
            if (direction == Vector2.Zero) return rectangle;
            var translated = rectangle;
            translated.Position += direction;
            return rectangle.Union(translated);

            // rectangle.Position += direction;
            // var translation =  Matrix3x2.CreateTranslation(new System.Numerics.Vector2(direction.X, direction.Y));
            //
            // rectangle.
            // var transformMatrix = Ma  Matrix2.CreateTranslation(direction);
            // var translated = RectangleF.Transform(rectangle, ref transformMatrix);
            // // Console.WriteLine($"{rectangle}, {translated}, {transformMatrix}");
            // // return RectangleF.Union(rectangle, translated);
            // return translated;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CircleF Inflate(this CircleF circle, float addRadius) =>
            new CircleF(circle.Center, circle.Radius + addRadius);

        public static void Wrap(this Transform2 transform, RectangleF levelSize)
        {
            if (transform.Parent != null) return;

            if (transform.Position.X < 0) {
                transform.Position += levelSize.WidthVector();
            } else if (transform.Position.X >= levelSize.Width) {
                transform.Position -= levelSize.WidthVector();
            }

            if (transform.Position.Y < 0) {
                transform.Position += levelSize.HeightVector();
            } else if (transform.Position.Y >= levelSize.Height) {
                transform.Position -= levelSize.HeightVector();
            }
        }


        /// <summary>
        ///     Calculate a's penetration into b
        /// </summary>
        /// <param name="a">The penetrating shape.</param>
        /// <param name="b">The shape being penetrated.</param>
        /// <returns>The distance vector from the edge of b to a's Position</returns>
        public static Vector2 CalculatePenetrationVector(this IShapeF a, IShapeF b)
        {
            if (!a.Intersects(b)) return Vector2.Zero;
            var penetrationVector = a switch {
                RectangleF rectA when b is RectangleF rectB => PenetrationVector(rectA, rectB),
                CircleF circA when b is CircleF circB => PenetrationVector(circA, circB),
                CircleF circA when b is RectangleF rectB => PenetrationVector(circA, rectB),
                RectangleF rectA when b is CircleF circB => PenetrationVector(rectA, circB),
                _ => throw new NotSupportedException("Shapes must be either a CircleF or RectangleF")
            };
            return penetrationVector;
        }

        private static Vector2 PenetrationVector(RectangleF rect1, RectangleF rect2)
        {
            var intersectingRectangle = RectangleF.Intersection(rect1, rect2);
            if (intersectingRectangle.IsEmpty) return Vector2.Zero;

            Vector2 penetration;
            if (intersectingRectangle.Width < intersectingRectangle.Height) {
                var d = rect1.Center.X < rect2.Center.X
                    ? intersectingRectangle.Width
                    : -intersectingRectangle.Width;
                penetration = new Vector2(d, 0);
            } else {
                var d = rect1.Center.Y < rect2.Center.Y
                    ? intersectingRectangle.Height
                    : -intersectingRectangle.Height;
                penetration = new Vector2(0, d);
            }

            return penetration;
        }

        private static Vector2 PenetrationVector(CircleF circ1, CircleF circ2)
        {
            if (!circ1.Intersects(circ2)) {
                return Vector2.Zero;
            }

            var displacement = Point2.Displacement(circ1.Center, circ2.Center);

            Vector2 desiredDisplacement;
            if (displacement != Vector2.Zero) {
                desiredDisplacement = displacement.NormalizedCopy() * (circ1.Radius + circ2.Radius);
            } else {
                desiredDisplacement = -Vector2.UnitY * (circ1.Radius + circ2.Radius);
            }


            var penetration = displacement - desiredDisplacement;
            return penetration;
        }

        private static Vector2 PenetrationVector(CircleF circ, RectangleF rect)
        {
            var collisionPoint = rect.ClosestPointTo(circ.Center);
            var cToCollPoint = collisionPoint - circ.Center;

            if (rect.Contains(circ.Center) || cToCollPoint.Equals(Vector2.Zero)) {
                var displacement = Point2.Displacement(circ.Center, rect.Center);

                Vector2 desiredDisplacement;
                if (displacement != Vector2.Zero) {
                    // Calculate penetration as only in X or Y direction.
                    // Whichever is lower.
                    var dispx = new Vector2(displacement.X, 0);
                    var dispy = new Vector2(0, displacement.Y);
                    dispx.Normalize();
                    dispy.Normalize();

                    dispx *= circ.Radius + rect.Width / 2;
                    dispy *= circ.Radius + rect.Height / 2;

                    if (dispx.LengthSquared() < dispy.LengthSquared()) {
                        desiredDisplacement = dispx;
                        displacement.Y = 0;
                    } else {
                        desiredDisplacement = dispy;
                        displacement.X = 0;
                    }
                } else {
                    desiredDisplacement = -Vector2.UnitY * (circ.Radius + rect.Height / 2);
                }

                var penetration = displacement - desiredDisplacement;
                return penetration;
            } else {
                var penetration = circ.Radius * cToCollPoint.NormalizedCopy() - cToCollPoint;
                return penetration;
            }
        }

        private static Vector2 PenetrationVector(RectangleF rect, CircleF circ)
        {
            return -PenetrationVector(circ, rect);
        }

        public static Vector2 GetSupportVector(this IShapeF shape, Vector2 direction) =>
            shape switch {
                RectangleF rect => rect.ClosestPointTo(rect.Center + direction),
                CircleF circle => circle.ClosestPointTo(circle.Center + direction),
                _ => throw new NotImplementedException()
            };

        public static Point2 GetCenter(this IShapeF shape) => shape switch {
            RectangleF rect => rect.Center,
            CircleF circle => circle.Center,
            _ => throw new NotImplementedException()
        };

        public static OverlapTestResult TestOverlap(this IShapeF first, IShapeF second)
        {
            var direction = Vector2.Zero;
            var vertices = new List<Vector2>();
            var result = EvolveResult.StillEvolving;
            var iterations = 0;
            while (iterations < 20 && result == EvolveResult.StillEvolving) {
                result = EvolveSimplex(vertices, first, second, ref direction);
                iterations++;
            }

            return new OverlapTestResult(result == EvolveResult.FoundIntersection, vertices, first, second);
        }

        private static Vector2 TripleProduct(Vector2 a, Vector2 b, Vector2 c)
        {
            var a3 = new Vector3(a.X, a.Y, 0);
            var b3 = new Vector3(b.X, b.Y, 0);
            var c3 = new Vector3(c.X, c.Y, 0);

            var first = Vector3.Cross(a3, b3);
            var second = Vector3.Cross(first, c3);

            return new Vector2(second.X, second.Y);
        }

        private static EvolveResult EvolveSimplex(List<Vector2> vertices, IShapeF shapeA, IShapeF shapeB,
            ref Vector2 direction)
        {
            switch (vertices.Count) {
                case 0:
                    direction = shapeB.GetCenter() - shapeA.GetCenter();
                    break;

                // flip the direction
                case 1:
                    direction *= 1;
                    break;
                case 2: {
                    // line ab is the line formed by the first two vertices
                    var ab = vertices[1] - vertices[0];
                    // line a0 is the line from the first vertex to the origin
                    var a0 = vertices[0] * -1;

                    // use the triple-cross-product to calculate a direction perpendicular
                    // to line ab in the direction of the origin
                    direction = TripleProduct(ab, a0, ab);
                    break;
                }
                case 3: {
                    // calculate if the simplex contains the origin
                    var c0 = vertices[2] * -1;
                    var bc = vertices[1] - vertices[2];
                    var ca = vertices[0] - vertices[2];

                    var bcNormal = TripleProduct(ca, bc, bc);
                    var caNormal = TripleProduct(bc, ca, ca);

                    if (Vector2.Dot(bcNormal, c0) > 0) {
                        // the origin is outside line bc
                        // get rid of a and add a new support in the direction of bcNorm
                        vertices.Remove(vertices[0]);
                        direction = bcNormal;
                    } else if (Vector2.Dot(caNormal, c0) > 0) {
                        // the origin is outside line ca
                        // get rid of b and add a new support in the direction of caNorm
                        vertices.Remove(vertices[1]);
                        direction = caNormal;
                    } else {
                        // the origin is inside both ab and ac,
                        // so it must be inside the triangle!
                        return EvolveResult.FoundIntersection;
                    }

                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(vertices),
                        $"Can't have simplex with {vertices.Count} vertixes!");
            }

            return AddSupport(vertices, shapeA, shapeB, direction)
                ? EvolveResult.StillEvolving
                : EvolveResult.NoIntersection;
        }

        private static bool AddSupport(List<Vector2> vertices, IShapeF shapeA, IShapeF shapeB, Vector2 direction)
        {
            var newVertex = CalculateSupport(shapeA, shapeB, direction);
            vertices.Add(newVertex);
            return direction.Dot(newVertex) >= 0;
        }

        private static Vector2 CalculateSupport(IShapeF shapeA, IShapeF shapeB, Vector2 direction)
        {
            var oppositeDirection = direction * -1;
            var newVertex = shapeA.GetSupportVector(direction);
            newVertex -= shapeB.GetSupportVector(oppositeDirection);
            return newVertex;
        }


        private enum EvolveResult
        {
            NoIntersection,
            FoundIntersection,
            StillEvolving
        }
    }


    public class OverlapTestResult
    {
        public bool Colliding { get; set; }
        public List<Vector2> Simplex { get; set; }
        public IShapeF ShapeA { get; set; }
        public IShapeF ShapeB { get; set; }

        public OverlapTestResult(bool colliding, List<Vector2> simplex, IShapeF shapeA, IShapeF shapeB)
        {
            Colliding = colliding;
            Simplex = simplex;
            ShapeA = shapeA;
            ShapeB = shapeB;
        }
    }
}
