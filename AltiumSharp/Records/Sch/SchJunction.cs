﻿using System;
using AltiumSharp.BasicTypes;

namespace AltiumSharp.Records
{
    internal class SchJunction : SchGraphicalObject
    {
        public override int Record => 29;
        public LineWidth Size { get; internal set; }
        public bool Locked { get; internal set; }

        public bool IsManualJunction => IndexInSheet != -1;

        public override CoordRect CalculateBounds() =>
            new CoordRect(Location.X - Utils.DxpToCoord(2), Location.Y - Utils.DxpToCoord(2), Utils.DxpToCoord(4), Utils.DxpToCoord(4));

        public override void ImportFromParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ImportFromParameters(p);
            Size = p["SIZE"].AsEnumOrDefault<LineWidth>();
            Locked = p["LOCKED"].AsBool();
        }

        public override void ExportToParameters(ParameterCollection p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            base.ExportToParameters(p);
            p.Add("SIZE", Size);
            p.Add("LOCKED", Locked);
        }
    }
}