﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Animation;

namespace MobileMuni
{
    interface IEntranceExitAnimation
    {
        Storyboard EntranceAnimation { get; }
    }
}
