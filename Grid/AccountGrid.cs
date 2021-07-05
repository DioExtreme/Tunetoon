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

        public AccountGrid()
        {
            DoubleBuffered = true;
        }

        public void ClearGridSelections()
        {
            ClearSelection();
            CurrentCell = null;
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

            if ((e.Button & MouseButtons.Left) == MouseButtons.Left && clickedRowIndex >= 0)
            {
                DoDragDrop(Rows[clickedRowIndex], DragDropEffects.Move);
            }

            base.OnMouseMove(e);
        }
        

        // Checks login for an selected account
        protected override void OnSelectionChanged(EventArgs e)
        {
            if (MoveMode)
            {
                base.OnSelectionChanged(e);
                return;
            }

            foreach (DataGridViewRow row in SelectedRows)
            {
                if (row.IsNewRow)
                {
                    break;
                }
                var account = row.DataBoundItem as Account;
                if (account != null && !account.LoginWanted)
                {
                    account.LoginWanted = true;
                }
            }

            base.OnSelectionChanged(e);
        }

        // Unchecks login for an unselected account
        protected override void OnRowLeave(DataGridViewCellEventArgs e)
        {
            if (MoveMode)
            {
                base.OnRowLeave(e);
                return;
            }

            DataGridViewRow row = Rows[e.RowIndex];
            var account = row.DataBoundItem as Account;

            if (!row.IsNewRow && row.Selected)
            {      
                account.LoginWanted = false;
            }

            base.OnRowLeave(e);
        }

        // Handles row dropping in another position
        protected override void OnDragDrop(DragEventArgs e)
        {
            Point clientPoint = PointToClient(new Point(e.X, e.Y));

            RowIndexToDrop = HitTest(clientPoint.X, clientPoint.Y).RowIndex;

            if (RowIndexToDrop < 0 || RowIndexToDrop == Rows.Count - 1)
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
