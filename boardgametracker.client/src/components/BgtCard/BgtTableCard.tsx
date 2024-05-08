import { ComponentPropsWithoutRef, Dispatch, SetStateAction, useEffect, useState } from 'react';

import { ColumnDef, getCoreRowModel, useReactTable } from '@tanstack/react-table';

import { Play } from '../../models';
import { BgtNoData } from '../BgtNoData/BgtNoData';
import { BgtBody, BgtHead, BgtPagination, BgtTable, BgtTableContainer } from '../BgtTable/BgtTable';
import { BgtCard } from './BgtCard';

interface Props extends ComponentPropsWithoutRef<'div'> {
  title: string;
  plays: Play[];
  columns: ColumnDef<Play, any>[];
  setPage: Dispatch<SetStateAction<number>>;
  currentPage: number;
  hasMore: boolean;
  totalPages: number;
}
export const BgtTableCard = (props: Props) => {
  const { plays, columns, title, setPage, currentPage, hasMore, totalPages, className } = props;
  const [data, setData] = useState<Play[]>([]);

  useEffect(() => {
    setData(plays);
  }, [plays]);

  const table = useReactTable({
    data,
    columns,
    getCoreRowModel: getCoreRowModel(),
  });

  if (plays.length === 0) {
    return (
      <BgtCard title={title} className={className}>
        <BgtNoData />
      </BgtCard>
    );
  }

  return (
    <BgtCard title={title} className={className}>
      <BgtTableContainer>
        <BgtTable>
          <BgtHead headers={table.getHeaderGroups()} />
          <BgtBody rows={table.getRowModel().rows} />
        </BgtTable>
        <BgtPagination setPage={setPage} currentPage={currentPage} hasMore={hasMore} totalPages={totalPages} />
      </BgtTableContainer>
    </BgtCard>
  );
};
