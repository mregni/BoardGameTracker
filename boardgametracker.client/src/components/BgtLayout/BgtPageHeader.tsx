import { useTranslation } from 'react-i18next';
import { ReactElement } from 'react';

import { BgtIconButton } from '../BgtIconButton/BgtIconButton';
import { BgtHeading } from '../BgtHeading/BgtHeading';
import BgtButton from '../BgtButton/BgtButton';
import { Actions } from '../../models';

import ArrowLeft from '@/assets/icons/arrow-left.svg?react';

interface Props {
  header: string;
  children?: ReactElement | ReactElement[];
  actions: Actions[];
  backAction?: () => void;
}

const BgtPageHeader = (props: Props) => {
  const { header, actions, backAction } = props;
  const { t } = useTranslation();

  return (
    <div className="flex-auto flex justify-between">
      <div className="flex flex-row gap-3 content-center items-center">
        {backAction && <BgtIconButton size="big" intent="header" icon={<ArrowLeft />} onClick={() => backAction()} />}
        <BgtHeading>{header}</BgtHeading>
      </div>
      <div className="flex content-center flex-wrap gap-3">
        {actions.map((x, i) => (
          <BgtButton key={i} variant={x.variant} size="3" onClick={x.onClick}>
            {t(x.content)}
          </BgtButton>
        ))}
      </div>
    </div>
  );
};

export default BgtPageHeader;
