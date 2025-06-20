import { useTranslation } from 'react-i18next';
import { PropsWithChildren, ReactElement } from 'react';

import { BgtIconButton } from '../BgtIconButton/BgtIconButton';
import { BgtHeading } from '../BgtHeading/BgtHeading';
import BgtButton from '../BgtButton/BgtButton';
import { Actions } from '../../models';

import ArrowLeft from '@/assets/icons/arrow-left.svg?react';

interface Props extends PropsWithChildren {
  header: string;
  children?: ReactElement | ReactElement[];
  actions: Actions[];
  backAction?: () => void;
}

const BgtPageHeader = (props: Props) => {
  const { header, actions, backAction, children = null } = props;
  const { t } = useTranslation();

  return (
    <div className="flex flex-col gap-2">
      <div className="flex-auto flex justify-between">
        <div className="flex flex-row gap-3 content-center items-center">
          {backAction && <BgtIconButton size="big" intent="header" icon={<ArrowLeft />} onClick={() => backAction()} />}
          <BgtHeading>{header}</BgtHeading>
        </div>
        <div className="flex content-center flex-wrap gap-3">
          <div className="hidden md:flex">{children}</div>
          {actions.map((x, i) => (
            <BgtButton key={i} variant={x.variant} size="3" onClick={x.onClick}>
              {t(x.content)}
            </BgtButton>
          ))}
        </div>
      </div>
      <div className="block md:hidden">{children}</div>
    </div>
  );
};

export default BgtPageHeader;
