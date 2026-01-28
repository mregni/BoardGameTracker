import { ReactNode } from 'react';

import { BgtCard } from '@/components/BgtCard/BgtCard';

interface Props {
  title: string;
  description: string;
  children: ReactNode;
}

export const SettingsSection = ({ title, description, children }: Props) => {
  return (
    <BgtCard title={title} description={description}>
      <div className="space-y-4">{children}</div>
    </BgtCard>
  );
};
