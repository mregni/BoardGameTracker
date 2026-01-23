import { useTranslation } from 'react-i18next';
import { PropsWithChildren } from 'react';

import { BgtIconButton } from '../BgtIconButton/BgtIconButton';
import { BgtHeading } from '../BgtHeading/BgtHeading';
import BgtButton from '../BgtButton/BgtButton';
import { Actions } from '../../models';

import ArrowLeft from '@/assets/icons/arrow-left.svg?react';

interface Props extends PropsWithChildren {
  header?: string;
  actions?: Actions[];
  backAction?: () => void;
  backText?: string;
}

export const BgtPageHeader = (props: Props) => {
  const { header, actions = [], backAction, backText, children = null } = props;
  const { t } = useTranslation();

  return (
    <div className="flex flex-col gap-2">
      <div className="flex-auto flex-col lg:flex-row flex justify-between max-lg:gap-2">
        <div className="flex flex-row gap-3 content-center items-center">
          {backAction && backText && (
            <BgtButton variant="text" onClick={backAction} className="pl-0">
              <ArrowLeft className="w-4 h-4" />
              {backText}
            </BgtButton>
          )}
          {backAction && !backText && (
            <BgtIconButton size="2" intent="header" icon={<ArrowLeft />} onClick={backAction} />
          )}
          {header && <BgtHeading>{header}</BgtHeading>}
        </div>
        <div className="flex items-center flex-wrap gap-3">
          <div className="hidden md:flex">{children}</div>
          {actions.map((action, index) => {
            const content = typeof action.content === 'string' ? t(action.content) : action.content;
            const smallContent = action.smallContent === undefined ? content : action.smallContent;

            return (
              <>
                <div key={`${index}-large`} className="hidden lg:block">
                  <BgtButton variant={action.variant} size="3" onClick={action.onClick}>
                    {content}
                  </BgtButton>
                </div>
                <div key={`${index}-small`} className="block lg:hidden">
                  <BgtButton variant={action.variant} size="1" onClick={action.onClick}>
                    {smallContent}
                  </BgtButton>
                </div>
              </>
            );
          })}
        </div>
      </div>
      <div className="block md:hidden">{children}</div>
    </div>
  );
};

export default BgtPageHeader;
