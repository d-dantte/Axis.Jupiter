using System;
using System.Collections;
using System.Collections.Generic;

namespace Axis.Jupiter.Utils
{
    public class SequencePage<Data> : IEnumerable<Data>
    {
        public long PageIndex { get; private set; }
        public long SequenceLength { get; private set; }
        public Data[] Page { get; private set; }

        public SequencePage(Data[] page, long pageIndex, long sequenceLength)
        {
            if (page == null || pageIndex < 0 || sequenceLength < 0) throw new Exception("invalid page");
            this.PageIndex = pageIndex;
            this.SequenceLength = sequenceLength;
            this.Page = page;
        }

        public IEnumerator<Data> GetEnumerator() => Page.GetEnumerator() as IEnumerator<Data>;
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
