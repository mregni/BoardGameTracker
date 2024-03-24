import {Children, ReactElement} from 'react';

import {BgtPageContent} from './BgtPageContent';
import BgtPageHeader from './BgtPageHeader';

interface Props {
  children: ReactElement | ReactElement[];
}

const checkComponentName = (child: React.ReactElement<any, string | React.JSXElementConstructor<any>>, elementName: string): boolean => {
  return (child.type as (props: Props) => JSX.Element)?.name === elementName;
}

export const BgtPage = (props: Props) => {
  const { children } = props;

  let _content, _header;
  Children.forEach(children, child => {
    if (checkComponentName(child, BgtPageHeader.name)) {
      return _header = child
    }

    if (checkComponentName(child, BgtPageContent.name)) {
      return _content = child
    }
  })

  return (
    <div className="w-full h-full flex flex-col gap-3 ">
      <div className='rounded-md bg-sky-900 p-3'>
        {_header}
      </div>
      <div className='overflow-y-auto'>
        {_content}
      </div>
    </div>
  )
}