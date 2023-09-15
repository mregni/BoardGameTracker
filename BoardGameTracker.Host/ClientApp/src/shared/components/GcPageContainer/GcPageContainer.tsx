import {ConfigProvider, Layout} from 'antd';
import React, {Children, ReactElement} from 'react';

import {purple} from '@ant-design/colors';

import {useScreenInfo} from '../../hooks/useScreenInfo';
import {GcPageContent} from './GcPageContent';
import {GcPageDrawer} from './GcPageDrawer';
import {GcPageHeader} from './GcPageHeader';

const { Header, Content} = Layout;

interface Props {
  children: ReactElement | ReactElement[];
}

const checkComponentName = (child: React.ReactElement<any, string | React.JSXElementConstructor<any>>, elementName: string): boolean => {
  return (child.type as (props: Props) => JSX.Element)?.name === elementName;
}

export const GcPageContainer = (props: Props) => {
  const { children } = props;
  const { screenMap } = useScreenInfo();

  let _content, _header, _drawers;

  Children.forEach(children, child => {
    if (checkComponentName(child, GcPageHeader.name)) {
      return _header = child
    }

    if (checkComponentName(child, GcPageContent.name)) {
      return _content = child
    } 

    if (checkComponentName(child, GcPageDrawer.name)) {
      return _drawers = child
    }
  })

  const height = screenMap.lg ? '64px' : '40px';

  return (
    <Layout style={{ height: '100%' }}>
      <ConfigProvider
        theme={{
          components: {
            Layout: {
              colorBgHeader: purple[8],
            }
          }
        }}
      >
        <Header style={{ paddingInline: screenMap.lg ? 20 : 10, height: height, lineHeight: height }}>
          {_header}
        </Header>
      </ConfigProvider>
      <Content style={{ padding: 12 }}>
        {_content}
        {_drawers}
      </Content>
    </Layout>
  )
}

