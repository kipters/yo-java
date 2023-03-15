package dev.kipters.yodemo.service;

import java.util.HashMap;
import java.util.HashSet;
import java.util.Map;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.security.core.authority.SimpleGrantedAuthority;
import org.springframework.security.core.userdetails.User;
import org.springframework.security.core.userdetails.UserDetails;
import org.springframework.security.core.userdetails.UserDetailsService;
import org.springframework.security.core.userdetails.UsernameNotFoundException;
import org.springframework.security.crypto.bcrypt.BCryptPasswordEncoder;
import org.springframework.stereotype.Service;

import software.amazon.awssdk.services.dynamodb.DynamoDbClient;
import software.amazon.awssdk.services.dynamodb.model.AttributeValue;
import software.amazon.awssdk.services.dynamodb.model.GetItemRequest;
import software.amazon.awssdk.services.dynamodb.model.PutItemRequest;

@Service("userService")
public class DynamoUserService implements UserDetailsService, IUserService {

    @Autowired
    private DynamoDbClient dynamo;
    
    @Autowired
    private BCryptPasswordEncoder passwordEncoder;

    @Override
    public UserDetails loadUserByUsername(String username) throws UsernameNotFoundException {
        var getItemRequest = GetItemRequest.builder()
            .key(Map.of("username", AttributeValue.builder().s(username).build()))
            .projectionExpression("username,password")
            .tableName("users")
            .build();

        var getResponse = dynamo.getItem(getItemRequest);
        var item = getResponse.item();

        if (item.size() == 0) {
            throw new UsernameNotFoundException("Username " + username + " not found");
        }

        var roles = new HashSet<SimpleGrantedAuthority>();
        roles.add(new SimpleGrantedAuthority("ROLE_USER"));
        var password = item.get("password").s();
        var details = new User(username, password, roles);

        return details;
    }

    @Override
    public Boolean addUser(String username, String password) {
        var getItemRequest = GetItemRequest.builder()
            .key(Map.of("username", AttributeValue.builder().s(username).build()))
            .tableName("users")
            .build();

        var getResponse = dynamo.getItem(getItemRequest);
        var item = getResponse.item();

        if (item.size() != 0) {
            return false;
        }

        var encodedPassword = passwordEncoder.encode(password);
        var itemValues = new HashMap<String, AttributeValue>();
        itemValues.put("username", AttributeValue.builder().s(username).build());
        itemValues.put("password", AttributeValue.builder().s(encodedPassword).build());

        var putItemRequest = PutItemRequest.builder()
            .tableName("users")
            .item(itemValues)
            .build();

        var response = dynamo.putItem(putItemRequest);
        
        return response.sdkHttpResponse().isSuccessful();
    }
    
}
