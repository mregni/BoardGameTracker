import { cx } from 'class-variance-authority';
import {
  useReactTable,
  getCoreRowModel,
  flexRender,
  getSortedRowModel,
  getFilteredRowModel,
  ColumnDef,
} from '@tanstack/react-table';

import { BgtTableRow, BgtTableHead, BgtTableCell, BgtTable, BgtTableHeader, BgtTableBody } from './BgtTable';

export interface DataTableProps<T> {
  columns: ColumnDef<T>[];
  data: T[];
  noDataMessage: string;
  size?: 'sm' | 'md';
  isLoading?: boolean;
  noHeaders?: boolean;
  firstCellClassNames?: string;
  widths?: string[];
}

export const BgtDataTable = <T,>(props: DataTableProps<T>) => {
  const { columns, data, size = 'md', noDataMessage, isLoading = false, widths, noHeaders } = props;
  const table = useReactTable({
    data,
    columns,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
    getFilteredRowModel: getFilteredRowModel(),
  });

  const headers = table.getHeaderGroups().map((header) => (
    <BgtTableRow key={header.id}>
      {header.headers.map((head, i) => (
        <BgtTableHead
          scope="col"
          key={head.id}
          className={cx(widths?.[i] ?? '', i === header.headers.length - 1 && 'text-right')}
        >
          {flexRender(head.column.columnDef.header, head.getContext())}
        </BgtTableHead>
      ))}
    </BgtTableRow>
  ));

  const cellClasses = cx('whitespace-nowrap', {
    'px-2.5 py-1 text-xs': size === 'sm',
    'px-3 py-3 text-sm': size === 'md',
  });

  let rows;

  if (isLoading) {
    rows = (
      <BgtTableRow>
        <BgtTableCell colSpan={columns.length} className="h-9">
          Loading data
        </BgtTableCell>
      </BgtTableRow>
    );
  }

  if (!isLoading && table.getRowModel().rows?.length) {
    rows = table.getRowModel().rows.map((row) => (
      <BgtTableRow key={row.id} data-state={row.getIsSelected() && 'selected'}>
        {row.getVisibleCells().map((cell, i) => (
          <BgtTableCell
            key={cell.id}
            className={cx(i === row.getVisibleCells().length - 1 && 'text-right', cellClasses)}
          >
            {flexRender(cell.column.columnDef.cell, cell.getContext())}
          </BgtTableCell>
        ))}
      </BgtTableRow>
    ));
  }

  if (!isLoading && table.getRowModel().rows?.length === 0) {
    rows = (
      <BgtTableRow>
        <BgtTableCell colSpan={columns.length} className="items-center text-center h-9">
          {noDataMessage}
        </BgtTableCell>
      </BgtTableRow>
    );
  }

  return (
    <BgtTable>
      {!noHeaders && <BgtTableHeader>{headers}</BgtTableHeader>}
      <BgtTableBody>{rows}</BgtTableBody>
    </BgtTable>
  );
};
