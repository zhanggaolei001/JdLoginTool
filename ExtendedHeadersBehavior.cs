﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;

namespace JdLoginTool.Wpf
{
    public class ExtendedHeadersBehavior : Behavior<DataGrid>
    {
        protected override void OnAttached()
        {
            AssociatedObject.AutoGeneratedColumns += AssociatedObject_AutoGeneratedColumns;
            AssociatedObject.AutoGeneratingColumn += AssociatedObject_AutoGeneratingColumn;
        }
        private readonly List<string> _displayOrder = new List<string>()
        {
           "建议再次登陆时间",
           "昵称","账号Pin",
           "手机号",
           "身份证2+4",
           "是否在线",
           "CK过期时间",
           "剩余有效时间",
           "vip",
           "京豆数量",
           "ck字符串",
           "地址列表",
           "用户信息详情"
        };
        private void AssociatedObject_AutoGeneratedColumns(object sender, EventArgs e)
        {
            var tmp = ((DataGrid)sender).Columns;
            var newColumns = tmp.OrderBy(dc => _displayOrder.IndexOf(dc.Header.ToString())).ToList();
            tmp.Clear();
            newColumns.ForEach(c =>
            {
                c.MaxWidth = 120;
                tmp.Add(c);
            });
        }

        protected override void OnDetaching()
        {
            AssociatedObject.AutoGeneratedColumns -= AssociatedObject_AutoGeneratedColumns;
            AssociatedObject.AutoGeneratingColumn -= AssociatedObject_AutoGeneratingColumn;
        }
        private void AssociatedObject_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {

            if (e.PropertyDescriptor is PropertyDescriptor desc)
            {
                if (desc.Attributes.OfType<BrowsableAttribute>().FirstOrDefault()?.Browsable == false)
                {
                    e.Cancel = true;
                }
                string header = desc.Attributes.OfType<DescriptionAttribute>()
                    .FirstOrDefault()?.Description;

                if (!string.IsNullOrEmpty(header))
                {
                    e.Column.Header = header;
                }
            }
        }
    }
}