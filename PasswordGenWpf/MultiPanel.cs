using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PasswordGenWpf
{
    public class MultiPanel : Panel
    {
        public MultiPanel()
        {

        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            UpdateChildVisibility();
        }

        private void UpdateChildVisibility()
        {
            foreach (UIElement child in InternalChildren)
            {
                child.Visibility = (CurrentPage != null && (GetPage(child) == CurrentPage)) ? Visibility.Visible : Visibility.Hidden;
            }
        }

        #region CurrentPage

        public static readonly DependencyProperty CurrentPageProperty = DependencyProperty.Register
        (
            "CurrentPage",
            typeof(string),
            typeof(MultiPanel),
            new FrameworkPropertyMetadata
            (
                "",
                FrameworkPropertyMetadataOptions.AffectsRender,
                new PropertyChangedCallback(OnCurrentPageChanged)
            )
        );

        public string CurrentPage
        {
            get { return (string)GetValue(CurrentPageProperty); }
            set { SetValue(CurrentPageProperty, value); }
        }

        private static void OnCurrentPageChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is MultiPanel)
            {
                ((MultiPanel)sender).OnCurrentPageChanged(e);
            }
        }

        protected virtual void OnCurrentPageChanged(DependencyPropertyChangedEventArgs e)
        {
            UpdateChildVisibility();
            InvalidateVisual();
        }

        #endregion

        #region Page (Attached Property)

        public static readonly DependencyProperty PageProperty = DependencyProperty.RegisterAttached
        (
            "Page",
            typeof(string),
            typeof(MultiPanel),
            new FrameworkPropertyMetadata
            (
                "",
                FrameworkPropertyMetadataOptions.AffectsRender,
                new PropertyChangedCallback(OnPageChanged)
            )
        );

        private static void OnPageChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            DependencyObject parent = null;
            do
            {
                parent = LogicalTreeHelper.GetParent(sender);
            }
            while (parent != null && !(parent is MultiPanel));

            if (parent is MultiPanel)
            {
                ((MultiPanel)parent).OnPageChanged(sender, e, true);
            }
        }

        protected virtual void OnPageChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e, bool dummy)
        {
            if (CurrentPage == (string)e.OldValue || CurrentPage == (string)e.NewValue)
            {
                UpdateChildVisibility();
                InvalidateVisual();
            }
        }

        public static void SetPage(UIElement element, string value)
        {
            element.SetValue(PageProperty, value);
        }

        public static string GetPage(UIElement element)
        {
            return (string)element.GetValue(PageProperty);
        }

        #endregion

        protected override Size MeasureOverride(Size availableSize)
        {
            Size desiredSize = new Size();

            foreach(UIElement child in InternalChildren)
            {
                child.Measure(availableSize);
                Size childDesiredSize = child.DesiredSize;
                desiredSize.Width = Math.Max(desiredSize.Width, childDesiredSize.Width);
                desiredSize.Height = Math.Max(desiredSize.Height, childDesiredSize.Height);
            }

            return desiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach(UIElement child in InternalChildren)
            {
                child.Arrange(new Rect(new Point(), finalSize));
            }

            return finalSize;
        }
    }
}
