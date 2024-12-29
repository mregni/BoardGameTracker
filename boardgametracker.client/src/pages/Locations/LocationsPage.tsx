import { useTranslation } from 'react-i18next';
import { useEffect, useState } from 'react';
import { getCoreRowModel, useReactTable } from '@tanstack/react-table';

import { Location } from '@/models';
import { usePage } from '@/hooks/usePage';
import { useLocations } from '@/hooks/useLocations';
import { BgtBody, BgtHead, BgtTable, BgtTableContainer } from '@/components/BgtTable/BgtTable';
import BgtPageHeader from '@/components/BgtLayout/BgtPageHeader';
import { BgtPageContent } from '@/components/BgtLayout/BgtPageContent';
import { BgtPage } from '@/components/BgtLayout/BgtPage';
import { BgtCard } from '@/components/BgtCard/BgtCard';

export const LocationsPage = () => {
  const { t } = useTranslation();
  const { pageTitle } = usePage();

  const [data, setData] = useState<Location[]>([]);

  const { locations } = useLocations();

  useEffect(() => {
    setData(locations);
  }, [locations]);

  const table = useReactTable({
    data,
    columns,
    getCoreRowModel: getCoreRowModel(),
  });

  return (
    <BgtPage>
      <BgtPageHeader header={t(pageTitle)} actions={[]} />
      <BgtPageContent>
        <BgtCard className="p-4">
          <BgtTableContainer>
            <BgtTable>
              <BgtHead headers={table.getHeaderGroups()} />
              <BgtBody rows={table.getRowModel().rows} />
            </BgtTable>
          </BgtTableContainer>
          {locations.map((x) => (
            <div key={x.id}>{x.name}</div>
          ))}
        </BgtCard>
      </BgtPageContent>
    </BgtPage>
  );
};
