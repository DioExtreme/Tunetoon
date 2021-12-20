using System;
using System.Drawing;
using System.Windows.Forms;
using Tunetoon.Accounts;

namespace Tunetoon.Grid
{
    public class AccountGrid : DataGridView
    {
        private int clickedRowIndex;

        public int RowIndexToDrop;
        public bool MoveMode;

        private int firstSelectedAccountIndex = -1;
        private int lastSelectedAccountIndex = -1;

        public AccountGrid()
        {
            DoubleBuffered = true;
        }

        public void ClearGridSelections()
        {
            ClearSelection();
            CurrentCell = null;
            firstSelectedAccountIndex = -1;
            lastSelectedAccountIndex = -1;
        }

        private bool RowIndexValid(int index)
        {
            return index >= 0 && index != NewRowIndex;
        }

        private bool IndexNextToFirstSelection(int index)
        {
            return index == firstSelectedAccountIndex + 1 || index == firstSelectedAccountIndex - 1;
        }

        private bool ReversingSelectDirection(int index)
        {
            return index == lastSelectedAccountIndex - 1 && index > firstSelectedAccountIndex ||
                   index == lastSelectedAccountIndex + 1 && index < firstSelectedAccountIndex;
        }

        // Fixes CellValueChanged not triggering
        protected override void OnCurrentCellDirtyStateChanged(EventArgs e)
        {
            if (IsCurrentCellDirty)
            {
                CommitEdit(DataGridViewDataErrorContexts.Commit);
            }

            base.OnCurrentCellDirtyStateChanged(e);
        }

        // Handles row dragging
        protected override void OnDragOver(DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;

            Point clientPoint = PointToClient(new Point(e.X, e.Y));

            int mousePositionY = clientPoint.Y;
            int mousePositionX = clientPoint.X;
            int rowIndexOfItemOver = HitTest(mousePositionX, mousePositionY).RowIndex;

            ClearSelection();

            // Signs where the dragged row will be dropped into
            if (rowIndexOfItemOver >= 0 && rowIndexOfItemOver < RowCount - 1)
            {
                Rows[rowIndexOfItemOver].Selected = true;
            }

            // Scroll down when we reach the last visible row with the mouse
            if (mousePositionY > Height * 0.95)
            {
                if (FirstDisplayedScrollingRowIndex < RowCount - 1)
                {
                    FirstDisplayedScrollingRowIndex += 1;
                }
            }

            // Likewise for scrolling up
            if (mousePositionY < Height * 0.09)
            {
                if (FirstDisplayedScrollingRowIndex > 0)
                {
                    FirstDisplayedScrollingRowIndex -= 1;
                }
            }

            base.OnDragOver(e);
        }

        // Clears selections when empty space is clicked
        protected override void OnMouseClick(MouseEventArgs e)
        {
            var ht = HitTest(e.X, e.Y);

            if (ht.Type == DataGridViewHitTestType.None)
            {
                ClearGridSelections();
            }

            base.OnMouseClick(e);
        }

        // Registers the index of the row we want to move
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (!MoveMode)
            {
                base.OnMouseDown(e);
                return;
            }

            clickedRowIndex = HitTest(e.X, e.Y).RowIndex;
            base.OnMouseDown(e);
        }

        // Dragging effect when moving a row
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (!MoveMode)
            {
                base.OnMouseMove(e);
                return;
            }

            if ((e.Button & MouseButtons.Left) == MouseButtons.Left && RowIndexValid(clickedRowIndex))
            {
                DoDragDrop(Rows[clickedRowIndex], DragDropEffects.Move);
            }

            base.OnMouseMove(e);
        }
        
        // Changes login intent for a selected account
        protected override void OnSelectionChanged(EventArgs e)
        {
            if (MoveMode || SelectedRows.Count == 0)
            {
                base.OnSelectionChanged(e);
                return;
            }

            // First index points to the currently selected account
            var row = SelectedRows[0];
            if (row.IsNewRow)
            {
                base.OnSelectionChanged(e);
                return;
            }

            if (SelectedRows.Count == 1)
            {
                firstSelectedAccountIndex = row.Index;
            }

            // If we were to undo a selection and then selected the same accounts again,
            // we'd be left with the first selection unchecked. Check it programatically.
            if (IndexNextToFirstSelection(row.Index))
            {
                var firstSelectedAccount = Rows[firstSelectedAccountIndex].DataBoundItem as Account;
                if (firstSelectedAccount != null)
                {
                    firstSelectedAccount.LoginWanted = true;
                }
            }

            // If we are undoing a selection, the last selected account will remain checked.
            // Uncheck it programatically.
            if (ReversingSelectDirection(row.Index))
            {
                var lastSelectedAccount = Rows[lastSelectedAccountIndex].DataBoundItem as Account;
                if (lastSelectedAccount != null)
                {
                    lastSelectedAccount.LoginWanted = false;
                }
            }

            lastSelectedAccountIndex = row.Index;

            var account = row.DataBoundItem as Account;
            if (account != null)
            {
                account.LoginWanted = !account.LoginWanted;
            }

            base.OnSelectionChanged(e);
        }

        // Handles row dropping in another position
        protected override void OnDragDrop(DragEventArgs e)
        {
            Point clientPoint = PointToClient(new Point(e.X, e.Y));

            RowIndexToDrop = HitTest(clientPoint.X, clientPoint.Y).RowIndex;

            if (!RowIndexValid(RowIndexToDrop))
            {
                base.OnDragDrop(e);
                return;
            }

            if (e.Effect == DragDropEffects.Move)
            {
                DataGridViewRow rowToMove = e.Data.GetData(typeof(DataGridViewRow)) as DataGridViewRow;
                BindingSource bindingSource = DataSource as BindingSource;

                if (bindingSource == null)
                {
                    base.OnDragDrop(e);
                    return;
                }

                var accToMove = bindingSource[rowToMove.Index];

                if (accToMove == null)
                {
                    base.OnDragDrop(e);
                    return;
                }

                bindingSource.RemoveAt(clickedRowIndex);
                bindingSource.Insert(RowIndexToDrop, accToMove);

                ClearGridSelections();
                Rows[RowIndexToDrop].Selected = true;
            }

            base.OnDragDrop(e);
        }
    }
}
