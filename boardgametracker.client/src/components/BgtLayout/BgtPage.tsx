import { Children, ReactElement } from 'react';

import { BgtPageHeader } from './BgtPageHeader';
import { BgtPageContent } from './BgtPageContent';

interface Props {
  children: ReactElement | ReactElement[];
}

const checkComponentName = (
  child: ReactElement<unknown, string | React.JSXElementConstructor<unknown>>,
  elementName: string
): boolean => {
  return (child.type as (props: Props) => JSX.Element)?.name === elementName;
};

export const BgtPage = (props: Props) => {
  const { children } = props;

  let content: ReactElement | undefined;
  let header: ReactElement | undefined;

  Children.forEach(children, (child) => {
    if (checkComponentName(child, BgtPageHeader.name)) {
      header = child;
    } else if (checkComponentName(child, BgtPageContent.name)) {
      content = child;
    }
  });

  return (
    <div className="w-full h-full flex flex-col p-3 xl:px-6 gap-3">
      {header}
      <div className="min-h-full">
        <div className="pb-3">{content}</div>
      </div>
    </div>
  );
};
