# Front-end estático do FlowDay servido pelo nginx.
FROM nginx:1.27-alpine
COPY . /usr/share/nginx/html
RUN mv /usr/share/nginx/html/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
