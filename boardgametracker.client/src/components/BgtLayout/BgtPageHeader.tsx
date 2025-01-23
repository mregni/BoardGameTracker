import { useTranslation } from 'react-i18next';
import { ReactElement } from 'react';

import { BgtHeading } from '../BgtHeading/BgtHeading';
import BgtButton from '../BgtButton/BgtButton';
import { Actions } from '../../models';

interface Props {
  header: string;
  children?: ReactElement | ReactElement[];
  backUrl?: string;
  actions: Actions[];
}

const BgtPageHeader = (props: Props) => {
  const { header, actions } = props;
  const { t } = useTranslation();

  return (
    <div className="flex-auto flex justify-between">
      <div className="flex flex-row gap-3 content-center items-center">
        <div>
          <BgtHeading>{header}</BgtHeading>
        </div>
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
