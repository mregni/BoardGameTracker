import clsx from 'clsx';
import { Dispatch, HTMLAttributes, ReactNode, SetStateAction } from 'react';

import { Button } from '@radix-ui/themes';
import { Cell, flexRender, HeaderGroup, Row } from '@tanstack/react-table';

interface ContainerProps {
  children: ReactNode;
}

interface HeadProps<T> extends HTMLAttributes<HTMLHeadElement> {
  headers: HeaderGroup<T>[];
}

interface BodyProps<T> {
  rows: Row<T>[];
}

interface RowProps<T> extends HTMLAttributes<HTMLTableRowElement> {
  row: Row<T>;
}

interface CellProps<T> extends HTMLAttributes<HTMLTableCellElement> {
  cell: Cell<T, unknown>;
}

interface PaginationProps {
  setPage: Dispatch<SetStateAction<number>>;
  hasMore: boolean;
  currentPage: number;
  totalPages: number;
}

export const BgtTableContainer = (props: ContainerProps) => {
  const { children } = props;
  return <div className="-mx-3 -mb-3">{children}</div>;
};

export const BgtTable = ({ className, ...props }: HTMLAttributes<HTMLTableElement>) => {
  return <table className={clsx('table-auto w-full', className)} {...props} />;
};

export const BgtHead = <T,>(props: HeadProps<T>) => {
  const { headers, className, ...rest } = props;
  return (
    <thead className={className} {...rest}>
      {headers.map((group) => (
        <tr key={group.id}>
          {group.headers.map((header) => (
            <th className="pb-3" key={header.id} colSpan={header.colSpan} style={{ width: header.getSize() }}>
              {header.isPlaceholder ? null : flexRender(header.column.columnDef.header, header.getContext())}
            </th>
          ))}
        </tr>
      ))}
    </thead>
  );
};

export const BgtBody = <T,>(props: BodyProps<T>) => {
  const { rows } = props;
  return (
    <tbody>
      {rows.map((row) => (
        <BgtRow key={row.id} row={row} />
      ))}
    </tbody>
  );
};

export const BgtRow = <T,>(props: RowProps<T>) => {
  const { row, className, ...rest } = props;
  return (
    <tr className={clsx('divide-x divide-sky-600 [&:nth-child(even)]:bg-sky-950', className)} {...rest}>
      {row.getVisibleCells().map((cell) => (
        <BgtCell key={cell.id} cell={cell} />
      ))}
    </tr>
  );
};

export const BgtCell = <T,>(props: CellProps<T>) => {
  const { cell, className, ...rest } = props;

  return (
    <td className={clsx('py-3 px-2 text-center', className)} {...rest}>
      {flexRender(cell.column.columnDef.cell, cell.getContext())}
    </td>
  );
};

export const BgtPagination = (props: PaginationProps) => {
  const { setPage, hasMore, currentPage, totalPages } = props;
  return (
    <div className="flex flex-row justify-between m-3 gap-2">
      <Button
        size="2"
        variant="solid"
        className="hover:cursor-pointer"
        disabled={currentPage === 0}
        onClick={() => setPage((old) => Math.max(old - 1, 0))}
      >
        Previous
      </Button>
      <div>
        {currentPage + 1} / {totalPages}
      </div>
      <Button size="2" variant="solid" className="hover:cursor-pointer" onClick={() => setPage((old) => old + 1)} disabled={!hasMore}>
        Next
      </Button>
    </div>
  );
};
