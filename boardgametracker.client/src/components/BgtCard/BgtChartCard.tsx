import { ComponentPropsWithoutRef, ComponentType, SVGProps } from 'react';

import { BgtCard } from './BgtCard';

interface Props extends ComponentPropsWithoutRef<'div'> {
  title: string;
  hide?: boolean;
  icon?: ComponentType<SVGProps<SVGSVGElement>>;
}

export const BgtChartCard = (props: Props) => {
  const { title, className, children, hide, icon, ...rest } = props;

  return (
    <BgtCard className={className} hide={hide} icon={icon} title={title} {...rest}>
      <div className="flex flex-col gap-3 p-3 h-full w-full">{children}</div>
    </BgtCard>
  );
};
