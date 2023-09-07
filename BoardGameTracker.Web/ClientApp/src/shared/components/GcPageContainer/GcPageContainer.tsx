import {Button, Col, ConfigProvider, Layout, Row, Space, Typography} from 'antd';
import useBreakpoint from 'antd/lib/grid/hooks/useBreakpoint';
import React, {Children, ReactElement, ReactNode} from 'react';
import {useNavigate} from 'react-router-dom';

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
  const navigate = useNavigate();
  const screens = useBreakpoint();

  return (
    <Row justify="space-between">
      <Row gutter={8} align="middle">
        {
          hasBack && (
            <Col>
              <Button type="text" icon={<ArrowLeftOutlined />} onClick={() => navigate(backNavigation)} />
            </Col>
          )
        }
        <Col>
          <Title level={screens.md ? 3 : 5} style={{ margin: 0 }}>{title}</Title>
        </Col>
      </Row>
      <Space wrap>
        {children}
      </Space>
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

