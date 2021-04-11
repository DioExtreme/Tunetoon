namespace Tunetoon.Grid
{
    public interface IGridHandler
    {
        void CellValueChanged(AccountGrid grid, int rowIndex);
        void DataBindingComplete(AccountGrid grid);
        void UserDeletingRow(object dataBoundItem);
    }
}
