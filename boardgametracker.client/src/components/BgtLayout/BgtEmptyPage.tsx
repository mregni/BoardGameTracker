import { ComponentType, PropsWithChildren, SVGProps } from 'react';

import BgtPageHeader from './BgtPageHeader';
import { BgtPageContent } from './BgtPageContent';
import { BgtPage } from './BgtPage';
import { BgtEmptyState } from './BgtEmptyState';

interface Props extends PropsWithChildren {
  header: string;
  icon: ComponentType<SVGProps<SVGSVGElement>>;
  emptyIcon?: ComponentType<SVGProps<SVGSVGElement>>;
  title: string;
  description: string;
  action?: {
    label: string;
    onClick: () => void;
  };
}

export const BgtEmptyPage = (props: Props) => {
  const { header, icon, emptyIcon, title, description, action, children } = props;

  return (
    <BgtPage>
      <BgtPageHeader header={header} icon={icon} />
      <BgtPageContent centered>
        <BgtEmptyState icon={emptyIcon ?? icon} title={title} description={description} action={action} />
        {children}
      </BgtPageContent>
    </BgtPage>
  );
};
