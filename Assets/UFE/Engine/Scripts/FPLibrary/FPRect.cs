using System;
using UnityEngine;

namespace FPLibrary
{
    /// <summary>
    /// A simple fixed point rect structure.
    /// </summary>
    [Serializable]
    public struct FPRect
    {
        public FPVector topRight;
        public FPVector topLeft;
        public FPVector bottomRight;
        public FPVector bottomLeft;
        public Fix64 x;
        public Fix64 y;
        public Fix64 width;
        public Fix64 height;
        public Fix64 xMax;
        public Fix64 yMax;

        public FPRect(Rect rect)
        {
            this.topLeft = new FPVector(rect.x, rect.y, 0);
            this.topRight = new FPVector(rect.xMax, rect.y, 0);
            this.bottomLeft = new FPVector(rect.x, rect.yMax, 0);
            this.bottomRight = new FPVector(rect.xMax, rect.yMax, 0);
            this.x = rect.x;
            this.y = rect.y;
            this.width = rect.width;
            this.height = rect.height;
            this.xMax = rect.xMax;
            this.yMax = rect.yMax;
        }

        public void MoveTo(FPVector fpVector)
        {
            this.x = fpVector.x;
            this.y = fpVector.y;
            RefreshPoints();
        }

        public void RefreshPoints()
        {
            this.xMax = this.x + this.width;
            this.yMax = this.y + this.height;
            this.topLeft = new FPVector(this.x, this.y, 0);
            this.topRight = new FPVector(this.xMax, this.y, 0);
            this.bottomLeft = new FPVector(this.x, this.yMax, 0);
            this.bottomRight = new FPVector(this.xMax, this.yMax, 0);
        }
        
        public bool Intersects(FPRect rect)
        {
            return rect.topLeft.x < this.topRight.x &&
                   this.topLeft.x < rect.topRight.x &&
                   rect.topLeft.y < this.bottomLeft.y &&
                   this.topLeft.y < rect.bottomLeft.y;
        }

        public Fix64 DistanceToPoint(FPVector point)
        {
            Fix64 xMax = this.topRight.x;
            Fix64 xMin = this.topLeft.x;
            Fix64 yMax = this.bottomRight.y;
            Fix64 yMin = this.topRight.y;

            if (point.x < xMin)
            { // Region I, VIII, or VII
                if (point.y < yMin)
                { // I
                    FPVector diff = point - new FPVector(xMin, yMin, 0);
                    return diff.magnitude;
                }
                else if (point.y > this.bottomRight.y)
                { // VII
                    FPVector diff = point - new FPVector(xMin, yMax, 0);
                    return diff.magnitude;
                }
                else
                { // VIII
                    return xMin - point.x;
                }
            }
            else if (point.x > xMax)
            { // Region III, IV, or V
                if (point.y < yMin)
                { // III
                    FPVector diff = point - new FPVector(xMax, yMin, 0);
                    return diff.magnitude;
                }
                else if (point.y > yMax)
                { // V
                    FPVector diff = point - new FPVector(xMax, yMax, 0);
                    return diff.magnitude;
                }
                else
                { // IV
                    return point.x - xMax;
                }
            }
            else
            { // Region II, IX, or VI
                if (point.y < yMin)
                { // II
                    return yMin - point.y;
                }
                else if (point.y > yMax)
                { // VI
                    return point.y - yMax;
                }
                else
                { // IX
                    return 0;
                }
            }
        }
    }
}