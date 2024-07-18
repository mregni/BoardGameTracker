import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ReactElement } from 'react';
import { IconButton } from '@radix-ui/themes';
import { ArrowLeftIcon } from '@heroicons/react/24/outline';

import { BgtHeading } from '../BgtHeading/BgtHeading';
import BgtButton from '../BgtButton/BgtButton';
import { Actions } from '../../models';

interface Props {
  header: string;
  subHeader?: string | null;
  children?: ReactElement | ReactElement[];
  hasBackButton?: boolean;
  backUrl?: string;
  actions: Actions[];
}

const BgtPageHeader = (props: Props) => {
  const { header, actions, hasBackButton = false, backUrl = '' } = props;
  const navigate = useNavigate();
  const { t } = useTranslation();

  return (
    <div className="flex-auto flex justify-between">
      <div className="flex flex-row gap-3 content-center items-center">
        {hasBackButton && (
          <IconButton variant="ghost" onClick={() => navigate(backUrl)}>
            <ArrowLeftIcon width="18" height="18" className="cursor-pointer" />
          </IconButton>
        )}
        <div>
          <BgtHeading className="uppercase">{header}</BgtHeading>
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
