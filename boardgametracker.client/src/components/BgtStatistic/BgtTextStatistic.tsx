import { cloneElement, isValidElement } from 'react';
import { cx } from 'class-variance-authority';

import { BgtText } from '../BgtText/BgtText';
import { BgtCard } from '../BgtCard/BgtCard';

interface Props {
  title: string;
  content: string | number | null;
  suffix?: string | number | null;
  prefix?: string | number | null;
  icon?: React.ReactNode;
  iconClassName?: string;
  textSize?: '1' | '2' | '3' | '4' | '5' | '6' | '7' | '8' | '9';
}

export const BgtTextStatistic = (props: Props) => {
  const { title, content, suffix, prefix, icon, iconClassName, textSize = '5' } = props;

  if (content === null || content === undefined) return null;

  const iconWithClasses =
    icon && isValidElement(icon)
      ? cloneElement(icon as React.ReactElement<{ className?: string }>, {
          className: cx('size-5', (icon.props as { className?: string }).className, iconClassName),
        })
      : icon;

  return (
    <BgtCard className="col-span-1">
      <div className="flex items-center gap-2 text-primary/70 uppercase mb-2">
        {iconWithClasses}
        <span>{title}</span>
      </div>
      <BgtText size={textSize} color="cyan" weight="bold">
        {prefix && <span>{prefix}&nbsp;</span>}
        {content.toLocaleString()}
        {suffix && <span className="text-sm lowercase">&nbsp;{suffix}</span>}
      </BgtText>
    </BgtCard>
  );
};
