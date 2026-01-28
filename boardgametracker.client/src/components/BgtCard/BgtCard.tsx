import { useTranslation } from 'react-i18next';
import { ComponentPropsWithoutRef, ComponentType, SVGProps } from 'react';
import { cx } from 'class-variance-authority';

import { BgtText } from '../BgtText/BgtText';
import BgtButton from '../BgtButton/BgtButton';

import { Actions } from '@/models';

interface Props extends ComponentPropsWithoutRef<'div'> {
  hide?: boolean;
  title?: string;
  icon?: ComponentType<SVGProps<SVGSVGElement>>;
  actions?: Actions[];
  description?: string;
}

export const BgtCard = (props: Props) => {
  const { children, className, hide, title, icon: Icon, actions = [], description, ...rest } = props;
  const { t } = useTranslation();

  if (hide) {
    return null;
  }

  return (
    <div className={cx('bg-primary/10 border border-primary/20 rounded-lg p-4 flex flex-col', className)} {...rest}>
      {title && (
        <div className="mb-4">
          <div className="flex justify-between items-start w-full">
            <h2 className="text-white uppercase tracking-wide flex items-center gap-2">
              {Icon && <Icon className="size-6 text-primary" />}
              {title}
            </h2>
            <div className="flex flex-row gap-2">
              {actions.map((action, index) => (
                <BgtButton key={index} variant={action.variant} size="2" onClick={action.onClick}>
                  {typeof action.content === 'string' ? t(action.content) : action.content}
                </BgtButton>
              ))}
            </div>
          </div>
          <BgtText size="2" color="white" opacity={50} className="mb-4">
            {description}
          </BgtText>
        </div>
      )}
      {children}
    </div>
  );
};
