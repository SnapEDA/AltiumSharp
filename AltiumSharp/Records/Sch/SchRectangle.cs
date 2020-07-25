using System;
using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    public class SchRectangle : SchGraphicalObject
    {
        public override int Record => 14;
        public CoordPoint Corner { get; internal set; }
        public LineWidth LineWidth { get; internal set; }
        public bool IsSolid { get; internal set; }
        public bool Transparent { get; internal set; }

        public override CoordRect CalculateBounds() =>
            new CoordRect(Location, Corner);

        public override void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ImportFromParameters(p);
            Corner = new CoordPoint(
                Utils.DxpFracToCoord(p["CORNER.X"].AsIntOrDefault(), p["CORNER.X_FRAC"].AsIntOrDefault()),
                Utils.DxpFracToCoord(p["CORNER.Y"].AsIntOrDefault(), p["CORNER.Y_FRAC"].AsIntOrDefault()));
            LineWidth = p["LINEWIDTH"].AsEnumOrDefault<LineWidth>();
            IsSolid = p["ISSOLID"].AsBool();
            Transparent = p["TRANSPARENT"].AsBool();
        }
        
        public override void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ExportToParameters(p);
            p.SetBookmark();
            {
                var (n, f) = Utils.CoordToDxpFrac(Corner.X);
                p.Add("CORNER.X", n);
                p.Add("CORNER.X_FRAC", f);
            }
            {
                var (n, f) = Utils.CoordToDxpFrac(Corner.Y);
                p.Add("CORNER.Y", n);
                p.Add("CORNER.Y_FRAC", f);
            }
            p.Add("LINEWIDTH", LineWidth);
            p.MoveKeys("COLOR");
            p.Add("ISSOLID", IsSolid);
            p.Add("TRANSPARENT", Transparent);
        }
    }
}
