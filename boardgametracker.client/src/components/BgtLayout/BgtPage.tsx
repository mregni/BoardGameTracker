import PerfectScrollbar from 'react-perfect-scrollbar';
import { Children, ReactElement } from 'react';

import BgtPageHeader from './BgtPageHeader';
import { BgtPageContent } from './BgtPageContent';

interface Props {
  children: ReactElement | ReactElement[];
}

const checkComponentName = (child: React.ReactElement<any, string | React.JSXElementConstructor<any>>, elementName: string): boolean => {
  return (child.type as (props: Props) => JSX.Element)?.name === elementName;
};

export const BgtPage = (props: Props) => {
  const { children } = props;

  let _content, _header;
  Children.forEach(children, (child) => {
    if (checkComponentName(child, BgtPageHeader.name)) {
      return (_header = child);
    }

    if (checkComponentName(child, BgtPageContent.name)) {
      return (_content = child);
    }
  });

  return (
    <div className="w-full h-full flex flex-col   ">
      {_header && <div className="p-3">{_header}</div>}

      <PerfectScrollbar
        options={{
          wheelSpeed: 0.7,
          suppressScrollX: true,
        }}
        className="md:pr-4"
      >
        {_content}
      </PerfectScrollbar>
    </div>
  );
};
