import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ReactElement } from 'react';
import { Button, Heading, IconButton } from '@radix-ui/themes';
import { ArrowLeftIcon } from '@heroicons/react/24/outline';

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
          <Heading as="h3" size="8" className="line-clamp-1 pr-2">
            {header}
          </Heading>
          {/* {subHeader && (
            <Text as="p" size="1">
              <span className="text-orange-600">{subHeader}</span>
            </Text>
          )} */}
        </div>
      </div>
      <div className="flex content-center flex-wrap gap-3">
        {actions.map((x, i) => (
          <Button key={i} variant={x.variant} size="3" onClick={x.onClick}>
            {t(x.content)}
          </Button>
        ))}
      </div>
    </div>
  );
};

export default BgtPageHeader;
