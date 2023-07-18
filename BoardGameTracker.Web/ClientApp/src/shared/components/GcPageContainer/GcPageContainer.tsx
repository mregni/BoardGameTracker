import {Button, Col, ConfigProvider, Layout, Row, Typography} from 'antd';
import React, {Children, ReactElement, ReactNode} from 'react';

import {purple} from '@ant-design/colors';
import {ArrowLeftOutlined} from '@ant-design/icons';

import {GcNoDataLoader} from '../GcNoDataLoader';

const { Header, Content } = Layout;
const { Title } = Typography;

interface HeaderProps {
  children: ReactNode;
  isLoading: boolean;
  title: string;
  hasBack?: boolean;
  backNavigation?: string;
}

export const GcPageContainerHeader = (props: HeaderProps) => {
  const { isLoading, children, title, hasBack = false, backNavigation = '' } = props;
  return (
    <Row justify="space-between">
      <Row gutter={8} align="middle">
        {
          hasBack && (
            <Col>
              <Button type="text" icon={<ArrowLeftOutlined />} />
            </Col>
          )
        }
        <Col>
          <Title level={3} style={{ margin: 0 }}>{title}</Title>
        </Col>
      </Row>
      <div>{children}</div>
    </Row>
  )
};

interface ContentProps {
  children: ReactNode;
  isLoading: boolean;
}

export const GcPageContainerContent = (props: ContentProps) => {
  const { isLoading, children } = props;
  return (
    <GcNoDataLoader isLoading={isLoading}>
      {children}
    </GcNoDataLoader>
  )
};


interface Props {
  children: ReactElement | ReactElement[];
}

const checkComponentName = (child: React.ReactElement<any, string | React.JSXElementConstructor<any>>, elementName: string): boolean => {
  return (child.type as (props: Props) => JSX.Element)?.name === elementName;
}

export const GcPageContainer = (props: Props) => {
  const { children } = props;
  let _content, _header;

  Children.forEach(children, child => {
    console.log(child.type);
    if (checkComponentName(child, GcPageContainerHeader.name)) {
      return _header = child
    }

    if (checkComponentName(child, GcPageContainerContent.name)) {
      return _content = child
    }
  })

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
        <Header style={{ paddingInline: 20 }}>
          {_header}
        </Header>
      </ConfigProvider>
      <Content style={{ padding: 12, height: '100%' }}>
        {_content}
      </Content>
    </Layout>
  )
}

