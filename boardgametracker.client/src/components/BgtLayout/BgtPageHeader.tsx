import { ReactElement } from 'react';
import { useNavigate } from 'react-router-dom';

import { ArrowLeftIcon } from '@heroicons/react/24/outline';
import { IconButton, Text } from '@radix-ui/themes';

interface Props {
  header: string;
  subHeader?: string | null;
  children?: ReactElement | ReactElement[];
  hasBackButton?: boolean;
  backUrl?: string;
}

const BgtPageHeader = (props: Props) => {
  const { header, subHeader = null, children, hasBackButton = false, backUrl = '' } = props;
  const navigate = useNavigate();

  return (
    <div className="flex-auto flex justify-between">
      <div className="flex flex-row gap-3 content-center items-center">
        {hasBackButton && (
          <IconButton variant="ghost" onClick={() => navigate(backUrl)}>
            <ArrowLeftIcon width="18" height="18" className="cursor-pointer" />
          </IconButton>
        )}
        <div>
          <Text as="p" className="line-clamp-1 text-lg md:text-xl pr-2">
            {header}
          </Text>
          {subHeader && (
            <Text as="p" size="1">
              <span className="text-orange-600">{subHeader}</span>
            </Text>
          )}
        </div>
      </div>
      <div className="flex content-center flex-wrap gap-3">{children}</div>
    </div>
  );
};

export default BgtPageHeader;
