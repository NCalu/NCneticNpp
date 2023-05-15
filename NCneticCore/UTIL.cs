using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace NCneticCore
{
    internal class ncTree<T> : IEnumerable<ncTree<T>>
    {
        #region public Fields
        public T Data { get; set; }
        public ncTree<T> Parent { get; set; }
        public ICollection<ncTree<T>> Children { get; set; }

        public Boolean IsRoot
        {
            get { return Parent == null; }
        }

        public Boolean IsLeaf
        {
            get { return Children.Count == 0; }
        }

        public int Level
        {
            get
            {
                if (this.IsRoot)
                    return 0;
                return Parent.Level + 1;
            }
        }
        #endregion

        #region constructors
        public ncTree(T data)
        {
            this.Data = data;
            this.Children = new LinkedList<ncTree<T>>();

            this.ElementsIndex = new LinkedList<ncTree<T>>();
            this.ElementsIndex.Add(this);
        }

        public ncTree<T> AddChild(T child)
        {
            ncTree<T> childNode = new ncTree<T>(child) { Parent = this };
            this.Children.Add(childNode);

            this.RegisterChildForSearch(childNode);

            return childNode;
        }
        #endregion

        #region searching

        private ICollection<ncTree<T>> ElementsIndex { get; set; }

        private void RegisterChildForSearch(ncTree<T> node)
        {
            ElementsIndex.Add(node);
            if (Parent != null)
                Parent.RegisterChildForSearch(node);
        }

        public ncTree<T> FindTreeNode(Func<ncTree<T>, bool> predicate)
        {
            return this.ElementsIndex.FirstOrDefault(predicate);
        }

        #endregion

        #region iterating

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<ncTree<T>> GetEnumerator()
        {
            yield return this;
            foreach (var directChild in this.Children)
            {
                foreach (var anyChild in directChild)
                    yield return anyChild;
            }
        }

        #endregion

        #region methods
        public IEnumerable<ncTree<T>> Flatten()
        {
            yield return this;

            foreach (var node in Children.SelectMany(child => child.Flatten()))
            {
                yield return node;
            }
        }
        #endregion
    }

    public class XmlColor
    {
        private Color color_ = Color.Black;

        public XmlColor() { }
        public XmlColor(Color c) { color_ = c; }


        public Color ToColor()
        {
            return color_;
        }

        public void FromColor(Color c)
        {
            color_ = c;
        }

        public static implicit operator Color(XmlColor x)
        {
            return x.ToColor();
        }

        public static implicit operator XmlColor(Color c)
        {
            return new XmlColor(c);
        }

        [XmlAttribute]
        public string Web
        {
            get { return ColorTranslator.ToHtml(color_); }
            set
            {
                try
                {
                    if (Alpha == 0xFF)
                        color_ = ColorTranslator.FromHtml(value);
                    else
                        color_ = Color.FromArgb(Alpha, ColorTranslator.FromHtml(value));
                }
                catch (Exception)
                {
                    color_ = Color.Black;
                }
            }
        }

        [XmlAttribute]
        public byte Alpha
        {
            get { return color_.A; }
            set
            {
                if (value != color_.A)
                    color_ = Color.FromArgb(value, color_);
            }
        }

        public bool ShouldSerializeAlpha() { return Alpha < 0xFF; }
    }
}
