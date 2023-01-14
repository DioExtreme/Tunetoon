using System.Collections.Generic;
using System.ComponentModel;

namespace Tunetoon.Accounts
{
    // Basically a BindingList that supports sorting
    public class AccountList<T>: BindingList<T> where T : Account
    {
        private bool isSorted;
        private ListSortDirection sortDirection;
        private PropertyDescriptor sortProperty;

        protected override ListSortDirection SortDirectionCore => sortDirection;

        protected override PropertyDescriptor SortPropertyCore => sortProperty;

        protected override bool SupportsSortingCore => true;

        protected override bool IsSortedCore => isSorted;

        private int Compare(T left, T right)
        {
            return left.Toon.CompareTo(right.Toon);
        }

        protected override void ApplySortCore(PropertyDescriptor prop, ListSortDirection direction)
        {
            var accounts = (List<T>)Items;
            accounts.Sort(Compare);

            if (direction == ListSortDirection.Descending)
            {
                accounts.Reverse();
            }

            isSorted = true;

            if (prop != null)
            {
                sortProperty = prop;
            }
            sortDirection = direction;

            ResetBindings();
        }

        protected override void RemoveSortCore()
        {
            isSorted = false;
            sortProperty = null;

            ResetBindings();
        }

        public void ApplySort()
        {
            if (sortDirection == ListSortDirection.Ascending)
            {
                ApplySortCore(null, ListSortDirection.Descending);
            }
            else
            {
                ApplySortCore(null, ListSortDirection.Ascending);
            }
        }

        public void RemoveSort()
        {
            RemoveSortCore();
        }
    }
}
